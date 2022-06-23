using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Services.Interfaces;
using Renci.SshNet;
using SixLabors.ImageSharp;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace Portfolio.Services;

public class BasicRemoteImageService : IRemoteImageService
{
    public string UploadProfileImage(IFormFile image, [Optional] string? fileName)
    {
        //set configuration
        var configuration = GetConfiguration();
        var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "ProfileUploadDirectory")!.Value;
        var storageDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "StorageUrl")!.Value;

        //upload picture based on the state of the 'fileName' parameter.
        string output;
        if (fileName != null && !fileName.Contains("Could not complete request."))
        {
            var result = UploadPicture(uploadDirectory, image, fileName);
            output = $"{storageDirectory}profilePictures/{result}";
        }
        else
        {
            var result = UploadPicture(uploadDirectory, image);
            output = $"{storageDirectory}profilePictures/{result}";
        }

        //return the result.
        return output;
    }

    public string UploadProjectImage(IFormFile image, [Optional] string? fileName)
    {
        //set configuration
        var configuration = GetConfiguration();
        var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "ProjectUploadDirectory")!.Value;
        var storageDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "StorageUrl")!.Value;

        //upload picture based on the state of the 'fileName' parameter.
        string output;
        if (fileName != null)
        {
            var result = UploadPicture(uploadDirectory, image, fileName);
            output = $"{storageDirectory}projectPictures/{result}";
        }
        else
        {
            var result = UploadPicture(uploadDirectory, image);
            output = $"{storageDirectory}projectPictures/{result}";
        }

        //return the result.
        return output;
    }

    public string UploadBlogImage(IFormFile image, [Optional] string? fileName)
    {
        //set configuration
        var configuration = GetConfiguration();
        var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "BlogUploadDirectory")!.Value;
        var storageDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "StorageUrl")!.Value;

        //upload picture based on the state of the 'fileName' parameter.
        string output;
        if (fileName != null)
        {
            var result = UploadPicture(uploadDirectory, image, fileName);
            output = $"{storageDirectory}blogPictures/{result}";
        }
        else
        {
            var result = UploadPicture(uploadDirectory, image);
            output = $"{storageDirectory}blogPictures/{result}";
        }

        //return the result.
        return output;
    }

    public string UploadPostImage(IFormFile image, [Optional] string? fileName)
    {
        //set configuration
        var configuration = GetConfiguration();
        var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "PostUploadDirectory")!.Value;
        var storageDirectory = configuration.GetSection("SftpSettings").GetChildren()
            .FirstOrDefault(d => d.Key == "StorageUrl")!.Value;

        //upload picture based on the state of the 'fileName' parameter.
        string output;
        if (fileName != null)
        {
            var result = UploadPicture(uploadDirectory, image, fileName);
            output = $"{storageDirectory}postPictures/{result}";
        }
        else
        {
            var result = UploadPicture(uploadDirectory, image);
            output = $"{storageDirectory}postPictures/{result}";
        }

        //return the result.
        return output;
    }

    public object UpdateImage(object modelToUpdate, IFormFile image)
    {
        //get model type
        var type = modelToUpdate.GetType();

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
                var fileExtension =
                    (modelToUpdate.GetType().GetProperty("FileName")!.GetValue(modelToUpdate) as string)!.Substring(
                        (modelToUpdate.GetType().GetProperty("FileName")!.GetValue(modelToUpdate) as string)!
                        .Length >= 4
                            ? (modelToUpdate.GetType().GetProperty("FileName")!.GetValue(modelToUpdate) as string)!
                            .Length - 4
                            : 0);

                //combine the found GUID with the file extension
                var fullFileName = $"{match.Value}{fileExtension}";

                //return URL from image upload
                var fileName = UploadImage(type, image, fullFileName);

                //get property metadata using reflection and set it.
                var prop = modelToUpdate.GetType()
                    .GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);

                //if the property is not null and is writable, use 'fileName' variable to set the property.
                if (null != prop && prop.CanWrite) prop.SetValue(modelToUpdate, fileName, null);
            }
            else
            {
                //upload image without file name.
                var fileName = UploadImage(type, image);

                //get property metadata using reflection and set it.
                var prop = modelToUpdate.GetType()
                    .GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);

                //if the property is not null and is writable, use 'fileName' variable to set the property.
                if (null != prop && prop.CanWrite) prop.SetValue(modelToUpdate, fileName, null);
            }
        }

        return modelToUpdate;
    }

    public void DeleteImage(object modelToDelete, string uploadDirectory)
    {
        //patter used to extract GUID
        var pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";

        //check if GUID is in URL
        var match = Regex.Match((modelToDelete.GetType().GetProperty("FileName")!.GetValue(modelToDelete) as string)!,
            pattern);

        //if there is a match preform the appropriate actions
        if (match.Success)
        {
            //get file extension
            var fileExtension =
                (modelToDelete.GetType().GetProperty("FileName")!.GetValue(modelToDelete) as string)!.Substring(
                    (modelToDelete.GetType().GetProperty("FileName")!.GetValue(modelToDelete) as string)!.Length >= 4
                        ? (modelToDelete.GetType().GetProperty("FileName")!.GetValue(modelToDelete) as string)!.Length -
                          4
                        : 0);

            //set full file name.
            var fileName = $"{match.Value}{fileExtension}";

            //Delete blog picture
            DeleteRemoteFile(uploadDirectory, fileName);
        }
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

    private string UploadImage(Type type, IFormFile image, [Optional] string? fullFileName)
    {
        //create 'fileName' variable.
        var fileName = string.Empty;

        //upload image based on type
        if (type == typeof(Blog) && fullFileName != null)
            fileName = UploadBlogImage(image, fullFileName);

        else if (type == typeof(Blog) && fullFileName == null)
            fileName = UploadBlogImage(image);

        else if (type == typeof(Post) && fullFileName != null)
            fileName = UploadPostImage(image, fullFileName);

        else if (type == typeof(Post) && fullFileName == null)
            fileName = UploadPostImage(image);

        else if (type == typeof(Project) && fullFileName != null)
            fileName = UploadProjectImage(image, fullFileName);

        else if (type == typeof(Project) && fullFileName == null)
            fileName = UploadProjectImage(image);

        //return url. 
        return fileName;
    }

    private SftpClient InitializeSftpClient()
    {
        //Syntax to login to file share...
        var configuration = GetConfiguration();
        var sshConfig = configuration.GetSection("SftpSettings").GetChildren();

        var configurationSections = sshConfig as IConfigurationSection[] ?? sshConfig.ToArray();
        var passPhrase = configurationSections.FirstOrDefault(c => c.Key == "PassPhrase")!.Value;
        var userName = configurationSections.FirstOrDefault(c => c.Key == "UserName")!.Value;
        var host = configurationSections.FirstOrDefault(c => c.Key == "Host")!.Value;

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

    private IConfigurationRoot GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()
            .Decrypt("CipherKey", "CipherText:");

        return configuration;
    }
}