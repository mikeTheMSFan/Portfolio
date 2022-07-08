namespace Portfolio.Models.Settings;

public class AppSettings
{
    public Azuread AzureAd { get; set; } = default!;
    public Downstreamapi DownstreamApi { get; set; } = default!;
    public Googleaccount GoogleAccount { get; set; } = default!;
    public Mailsettings MailSettings { get; set; } = default!;
    public Sftpsettings SftpSettings { get; set; } = default!;
}