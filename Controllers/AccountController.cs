using Microsoft.AspNetCore.Mvc;
using test_task_Mekashron.Contracts;
using test_task_Mekashron.Models.Payloads;
using test_task_Mekashron.Services;
using test_task_Mekashron.ViewModels;
using System.Linq;

namespace test_task_Mekashron.Controllers;

public class AccountController : Controller
{
    private readonly ISoapAuthService _soapAuthService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ISoapAuthService soapAuthService, ILogger<AccountController> logger)
    {
        _soapAuthService = soapAuthService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _soapAuthService.LoginAsync(
                model.Username.Trim(),
                model.Password.Trim(),
                GetClientIp(),
                HttpContext.RequestAborted);
            model.Result = result;

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }
        }
        catch (SoapRequestException ex)
        {
            _logger.LogWarning(ex, "SOAP login failed");
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected login error");
            ModelState.AddModelError(string.Empty, "Unexpected error while contacting the SOAP service.");
        }

        return View(model);
    }

    [HttpPost("api/account/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errors = ModelState
                    .Where(x => x.Value?.Errors?.Count > 0)
                    .Select(x => new { x.Key, message = x.Value!.Errors.First().ErrorMessage })
            });
        }

        try
        {
            var payload = new RegisterPayload
            {
                Email = request.Username.Trim(),
                Password = request.Password.Trim(),
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? request.Username.Trim() : request.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.LastName) ? request.Username.Trim() : request.LastName.Trim(),
                Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? "0000000000" : request.Mobile.Trim(),
                CountryId = request.CountryId <= 0 ? 1 : request.CountryId,
                AffiliateId = request.AffiliateId < 0 ? 0 : request.AffiliateId,
                SignupIp = string.IsNullOrWhiteSpace(request.SignupIp) ? GetClientIp() : request.SignupIp.Trim()
            };

            var result = await _soapAuthService.RegisterNewCustomerAsync(payload, HttpContext.RequestAborted);
            return Ok(new
            {
                success = result.Success,
                result.ResultCode,
                message = result.Message,
                raw = result.RawPayload
            });
        }
        catch (SoapRequestException ex)
        {
            _logger.LogWarning(ex, "SOAP register failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected register error");
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Unexpected error while contacting the SOAP service." });
        }
    }

    private string GetClientIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ip) && HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            ip = forwarded.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        return string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip;
    }
}
