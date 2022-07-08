namespace Portfolio.Models.Settings;

public class Sftpsettings
{
    public string BlogUploadDirectory { get; set; } = default!;
    public string Host { get; set; } = default!;
    public string PassPhrase { get; set; } = default!;
    public int Port { get; set; } = default!;
    public string PostUploadDirectory { get; set; } = default!;
    public string ProfileUploadDirectory { get; set; } = default!;
    public string ProjectUploadDirectory { get; set; } = default!;
    public string StorageUrl { get; set; } = default!;
    public string UserName { get; set; } = default!;
}