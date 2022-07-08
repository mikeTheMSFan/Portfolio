using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Portfolio.Models.Content;
using Portfolio.Models.Settings;
using Portfolio.Services.Interfaces;
using Portfolio.Enums;
using Renci.SshNet;
using SixLabors.ImageSharp;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace Portfolio.Services;

public class BasicRemoteImageService : IRemoteImageService
{
    private readonly AppSettings _appSettings;

    public BasicRemoteImageService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string UploadContentImage(IFormFile image, ContentType contentType, [Optional] string? fileName)
    {
        var paths = GetUploadDirectory(contentType);
        var storageUrl = _appSettings.SftpSettings.StorageUrl;

        //upload picture based on the state of the 'fileName' parameter.
        string output;
        if (fileName != null && !fileName.Contains("Could not complete request."))
        {
            var result = UploadPicture(paths.uploadDirectory, image, fileName);
            output = $"{storageUrl}{paths.uploadFolder}{result}";
        }
        else
        {
            var result = UploadPicture(paths.uploadDirectory, image);
            output = $"{storageUrl}{paths.uploadFolder}{result}";
        }

        //return the result.
        return output;
    }

    public object UpdateImage(object modelToUpdate, ContentType contentType, IFormFile image)
    {
        //if there is a file name, complete the appropriate process.
        if (modelToUpdate.GetType().GetProperty("FileName")!.GetValue(modelToUpdate) as string != null)
        {
            //pattern used to extract GUID from url
            var pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";

            //check if GUID is in url
            var match = Regex.Match(
                (modelToUpdate.GetType().GetProperty("FileName")!.GetValue(modelToUpdate) as string)!, pattern);

            //if a GUID is found, complete the appropreate process
            if (match.Success)
            {
                //get the file extension of file
                var fileExtension = GetFileExtensionFromObject(modelToUpdate);

                //combine the found GUID with the file extension
                var fullFileName = $"{match.Value}{fileExtension}";

                //return URL from image upload
                var contentUrl = GetContentUrl(image, contentType, fullFileName);

                //get property metadata using reflection and set it.
                var prop = modelToUpdate.GetType()
                    .GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);

                //if the property is not null and is writable, use 'fileName' variable to set the property.
                if (null != prop && prop.CanWrite) prop.SetValue(modelToUpdate, contentUrl, null);
            }
            else
            {
                //upload image without file name.
                var contentUrl = GetContentUrl(image, contentType);

                //get property metadata using reflection and set it.
                var prop = modelToUpdate.GetType()
                    .GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);

                //if the property is not null and is writable, use 'fileName' variable to set the property.
                if (null != prop && prop.CanWrite) prop.SetValue(modelToUpdate, contentUrl, null);
            }
        }

        return modelToUpdate;
    }

    public void CheckForImageToDelete(object modelToDelete, string uploadDirectory)
    {
        //patter used to extract GUID
        var pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";

        //check if GUID is in URL
        var match = Regex.Match((modelToDelete.GetType().GetProperty("FileName")!.GetValue(modelToDelete) as string)!,
            pattern);

        if (match.Success)
        {
            //get file extension
            var fileExtension = GetFileExtensionFromObject(modelToDelete);
            
            //Prepare image for deletion
            var fileName = PrepareFileForDeletion(match, fileExtension);

            //Delete blog picture
            DeleteRemoteFile(uploadDirectory, fileName);
        }
    }

    private string PrepareFileForDeletion(Match match, string fileExtension)
    {
        //set full file name.
        return $"{match.Value}{fileExtension}";
    }

    public void DeleteRemoteFile(string directory, string fileName)
    {
        //create Sftp client
        var client = InitializeSftpClient();

        //connect to site
        client.Connect();

        //create path of deleted firle
        var deletionPath = $"{directory}{fileName}";

        //delete the file
        client.DeleteFile(deletionPath);

        //disconnect from site
        client.Disconnect();

        //dispose of the client.
        client.Dispose();
    }

    private string GetFileExtensionFromObject(Object model)
    {
        return (model.GetType().GetProperty("FileName")!.GetValue(model) as string)!.Substring(
            (model.GetType().GetProperty("FileName")!.GetValue(model) as string)!
            .Length >= 4
                ? (model.GetType().GetProperty("FileName")!.GetValue(model) as string)!
                .Length - 4
                : 0);
    }

    private (string uploadDirectory, string uploadFolder) GetUploadDirectory(ContentType contentType)
    {
        if (contentType == ContentType.Blog)
        {
            return (_appSettings.SftpSettings.BlogUploadDirectory, "blogPictures/");
        }

        else if (contentType == ContentType.Post)
        {
            return (_appSettings.SftpSettings.PostUploadDirectory, "postPictures/");
        }

        else if (contentType == ContentType.Profile)
        {
            return (_appSettings.SftpSettings.ProfileUploadDirectory, "profilePictures/");
        }

        else if (contentType == ContentType.Project)
        {
            return (_appSettings.SftpSettings.ProjectUploadDirectory, "projectPictures/");
        }

        return (string.Empty, String.Empty);
    }

    private string GetContentUrl(IFormFile image, ContentType contentType, [Optional] string? fullPath)
    {
        //create 'contentURL' variable.
        var contentUrl = string.Empty;

        //upload image based on type
        if (fullPath != null)
            contentUrl = UploadContentImage(image, contentType, fullPath);

        else if (fullPath == null)
            contentUrl = UploadContentImage(image, contentType);

        //return url. 
        return contentUrl;
    }

    private SftpClient InitializeSftpClient()
    {
        var passPhrase = _appSettings.SftpSettings.PassPhrase;
        var userName = _appSettings.SftpSettings.UserName;
        var host = _appSettings.SftpSettings.Host;

        var connectionInfo = new ConnectionInfo(host,
            userName,
            new PasswordAuthenticationMethod(userName, passPhrase));

        var client = new SftpClient(connectionInfo);

        return client;
    }

    private string UploadPicture(string directory, IFormFile image, [Optional] string fileName)
    {
        var client = InitializeSftpClient();
        try
        {
            if (!string.IsNullOrEmpty(fileName)) DeleteRemoteFile(directory, fileName);

            var profileImage = Image.Load(image.OpenReadStream(), out var format);
            var newFileName = $"{Guid.NewGuid()}.{format.FileExtensions.FirstOrDefault()}";

            var memoryStream = new MemoryStream();
            memoryStream.Seek(0, SeekOrigin.Begin);
            profileImage.Save(memoryStream, format);

            client.Connect();
            var path = $"{directory}{newFileName}";
            memoryStream.Seek(0, SeekOrigin.Begin);

            client.UploadFile(memoryStream, path);
            client.Disconnect();
            client.Dispose();

            return newFileName;
        }
        catch (Exception)
        {
            if (client.IsConnected)
            {
                client.Disconnect();
                client.Dispose();
            }

            return "Could not complete request.";
        }
    }
}