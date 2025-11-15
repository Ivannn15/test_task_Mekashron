using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using test_task_Mekashron.Models;
using test_task_Mekashron.Models.Payloads;
using test_task_Mekashron.Options;

namespace test_task_Mekashron.Services;

public sealed class SoapAuthService : ISoapAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SoapServiceOptions _options;
    private readonly ILogger<SoapAuthService> _logger;

    public SoapAuthService(HttpClient httpClient, IOptions<SoapServiceOptions> options, ILogger<SoapAuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new ArgumentException("SOAP endpoint is not configured.");
        }
    }

    public async Task<LoginResult> LoginAsync(string username, string password, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var payload = await SendAsync("Login", new Dictionary<string, string>
        {
            ["UserName"] = username,
            ["Password"] = password,
            ["IPs"] = NormalizeIp(ipAddress)
        }, cancellationToken);

        var interpreted = InterpretPayload(payload);

        return new LoginResult
        {
            Success = interpreted.success,
            Message = interpreted.message,
            ResultCode = interpreted.code,
            Details = interpreted.details,
            RawPayload = interpreted.raw
        };
    }

    public async Task<RegisterResult> RegisterNewCustomerAsync(RegisterPayload payloadModel, CancellationToken cancellationToken = default)
    {
        var payload = await SendAsync("RegisterNewCustomer", new Dictionary<string, string>
        {
            ["Email"] = payloadModel.Email,
            ["Password"] = payloadModel.Password,
            ["FirstName"] = payloadModel.FirstName,
            ["LastName"] = payloadModel.LastName,
            ["Mobile"] = payloadModel.Mobile,
            ["CountryID"] = payloadModel.CountryId.ToString(),
            ["aID"] = payloadModel.AffiliateId.ToString(),
            ["SignupIP"] = NormalizeIp(payloadModel.SignupIp)
        }, cancellationToken);

        var interpreted = InterpretPayload(payload);

        return new RegisterResult
        {
            Success = interpreted.success,
            Message = interpreted.message,
            ResultCode = interpreted.code,
            RawPayload = interpreted.raw
        };
    }

    private async Task<string> SendAsync(string actionName, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(actionName, parameters);
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };

        if (_options.UseSoapActionHeader)
        {
            request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{_options.Namespace}#{actionName}\"");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SOAP action {Action} failed with status {Status}: {Body}", actionName, response.StatusCode, responsePayload);
            throw new SoapRequestException($"SOAP action '{actionName}' failed with status {(int)response.StatusCode}." +
                                           ($" Response body: {responsePayload}"));
        }

        return ExtractResultPayload(responsePayload);
    }

    private string BuildEnvelope(string actionName, IReadOnlyDictionary<string, string> parameters)
    {
        var inner = new StringBuilder();
        foreach (var kvp in parameters)
        {
            inner.Append('<').Append(kvp.Key).Append('>')
                .Append(SecurityElement.Escape(kvp.Value))
                .Append("</").Append(kvp.Key).Append('>');
        }

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <{actionName} xmlns=""{_options.Namespace}"">
      {inner}
    </{actionName}>
  </soap:Body>
</soap:Envelope>";
    }

    private static string NormalizeIp(string? ip)
        => string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip;

    private static string ExtractResultPayload(string soapResponse)
    {
        if (string.IsNullOrWhiteSpace(soapResponse))
        {
            return string.Empty;
        }

        try
        {
            var document = XDocument.Parse(soapResponse);
            var resultNode = document
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName.EndsWith("Result", StringComparison.OrdinalIgnoreCase));

            if (resultNode is null)
            {
                resultNode = document
                    .Descendants()
                    .FirstOrDefault(x => string.Equals(x.Name.LocalName, "return", StringComparison.OrdinalIgnoreCase));
            }

            return resultNode?.Value ?? soapResponse;
        }
        catch (Exception)
        {
            return soapResponse;
        }
    }

    private (bool success, int? code, string message, IReadOnlyList<SoapUserDetail> details, string raw) InterpretPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return (false, null, "Ответ сервиса пуст", Array.Empty<SoapUserDetail>(), string.Empty);
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            int? resultCode = null;
            string? resultMessage = null;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("ResultCode", out var codeElement) && codeElement.TryGetInt32(out var parsedCode))
                {
                    resultCode = parsedCode;
                }

                if (root.TryGetProperty("ResultMessage", out var messageElement))
                {
                    resultMessage = messageElement.GetString();
                }
            }

            var details = new List<SoapUserDetail>();
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("ResultCode") || property.NameEquals("ResultMessage"))
                    {
                        continue;
                    }

                    details.AddRange(Flatten(property.Value, property.Name));
                }
            }

            if (details.Count == 0)
            {
                details.Add(new SoapUserDetail("raw", root.GetRawText()));
            }

            var success = !resultCode.HasValue || resultCode.Value >= 0;
            var message = string.IsNullOrWhiteSpace(resultMessage)
                ? (success ? "Operation completed" : "Service returned an error")
                : resultMessage;

            return (success, resultCode, message, details, root.GetRawText());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SOAP JSON payload");
            return (false, null, "Не удалось распарсить ответ сервиса", new List<SoapUserDetail>
            {
                new("raw", payload)
            }, payload);
        }
    }

    private static IEnumerable<SoapUserDetail> Flatten(JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var nestedPrefix = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    foreach (var nested in Flatten(property.Value, nestedPrefix))
                    {
                        yield return nested;
                    }
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var nestedPrefix = $"{prefix}[{index}]";
                    foreach (var nested in Flatten(item, nestedPrefix))
                    {
                        yield return nested;
                    }
                    index++;
                }
                break;
            case JsonValueKind.Null:
                yield break;
            default:
                var value = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => element.GetRawText()
                };

                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return new SoapUserDetail(prefix, value);
                }
                break;
        }
    }
}
