using System.Runtime.InteropServices;
using Portfolio.Enums;

namespace Portfolio.Services.Interfaces;

public interface IRemoteImageService
{
    public string UploadContentImage(IFormFile image, ContentType contentType, [Optional] string fileName);
    public void DeleteRemoteFile(string directory, string fileName);
    public object UpdateImage(object modelToUpdate, ContentType contentType, IFormFile image);
    public void CheckForImageToDelete(object modelToDelete, string uploadDirectory);
}