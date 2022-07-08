namespace Portfolio.Models.Settings;

public class Clientcertificate
{
    public string SourceType { get; set; } = default!;
    public string KeyVaultUrl { get; set; } = default!;
    public string KeyVaultCertificateName { get; set; } = default!;
}