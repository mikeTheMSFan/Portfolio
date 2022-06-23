namespace Portfolio.Services.Interfaces;

public interface IExternalLogin
{
    public Task<string> GetMicrosoftGraphPhotoAsync(string token);
}