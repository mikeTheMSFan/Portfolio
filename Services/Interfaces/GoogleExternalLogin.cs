using System.Runtime.InteropServices;
using SixLabors.ImageSharp;

namespace Portfolio.Services.Interfaces;

public class GoogleExternalLogin: IExternalLoginService
{
    public async Task<string> GetBase64ExternalLoginPictureAsync([Optional]string token)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(token);
        var stream = await response.Content.ReadAsStreamAsync();
            
        var image = Image.Load(stream, out var format);
        return image.ToBase64String(format);
    }
}