using System.Runtime.InteropServices;

namespace Portfolio.Services.Interfaces;

public interface IExternalLoginService
{
    public Task<string> GetBase64ExternalLoginPictureAsync([Optional] string token);
}