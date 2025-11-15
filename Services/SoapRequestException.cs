namespace test_task_Mekashron.Services;

public sealed class SoapRequestException : Exception
{
    public SoapRequestException(string message) : base(message)
    {
    }

    public SoapRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
