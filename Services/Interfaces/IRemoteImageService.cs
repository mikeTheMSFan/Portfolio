using System.Runtime.InteropServices;

namespace Portfolio.Services.Interfaces;

public interface IRemoteImageService
{
    public string UploadProfileImage(IFormFile image, [Optional] string fileName);
    public string UploadProjectImage(IFormFile image, [Optional] string fileName);
    public string UploadBlogImage(IFormFile image, [Optional] string fileName);
    public string UploadPostImage(IFormFile image, [Optional] string fileName);
    public void DeleteRemoteFile(string directory, string fileName);
    public object UpdateImage(object modelToUpdate, IFormFile image);
    public void DeleteImage(object modelToDelete, string uploadDirectory);
}