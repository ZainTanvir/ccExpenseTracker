namespace CreditAnalyzer.Infrastructure.Options;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "statements";
    public bool WithSSL { get; set; } = false;
}