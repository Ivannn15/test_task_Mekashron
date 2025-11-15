namespace test_task_Mekashron.Options;

public sealed class SoapServiceOptions
{
    public const string SectionName = "SoapService";

    public string Endpoint { get; set; } = "http://isapi.mekashron.com/icu-tech/icutech-test.dll/soap/IICUTech";
    public string Namespace { get; set; } = "urn:ICUTech.Intf-IICUTech";
    public bool UseSoapActionHeader { get; set; } = true;
    public double TimeoutSeconds { get; set; } = 15;
}
