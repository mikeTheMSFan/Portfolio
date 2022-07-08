namespace Portfolio.Models.Settings;

public class Azuread
{
    public string CallBackPath { get; set; } = default!;
    public Clientcertificate[] ClientCertificates { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string Instance { get; set; } = default!;
    public string TenantId { get; set; } = default!;
}