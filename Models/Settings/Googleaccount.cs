namespace Portfolio.Models.Settings;

public class Googleaccount
{
    public string auth_provider_x509_cert_url { get; set; } = default!;
    public string auth_uri { get; set; } = default!;
    public string client_id { get; set; } = default!;
    public string client_secret { get; set; } = default!;
    public string project_id { get; set; } = default!;
    public string[] redirect_uris { get; set; } = default!;
    public string token_uri { get; set; } = default!;
}