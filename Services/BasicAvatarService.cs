using System.Runtime.InteropServices;
using Portfolio.Services.Interfaces;
using SixLabors.ImageSharp;

namespace Portfolio.Services;

public class BasicAvatarService : INoAvatarService
{
    public readonly IWebHostEnvironment HostEnvironment;

    public BasicAvatarService(IWebHostEnvironment hostEnvironment)
    {
        HostEnvironment = hostEnvironment;
    }

    public string GetAvatar()
    {
        //get server root path
        var serverRoot = HostEnvironment.WebRootPath;

        //create avatar path
        var avatarPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ? Path.Combine(serverRoot, @"imgs\avatars")
            : Path.Combine(serverRoot, @"imgs/avatars");

        //get new instance of directory info using avatar path.
        var d = new DirectoryInfo(avatarPath);

        //Create an array of file info by getting all png files.
        var files = d.GetFiles("*.png");

        //create an instance of random
        var random = new Random();

        //create a random integer using the number of files.
        var index = random.Next(files.Length);

        //load the random file using six labors
        var image = Image.Load(files[index].OpenRead(), out var format);

        //get create base64 image.
        var avatarBase64String = image.ToBase64String(format);

        //return base64 image.
        return avatarBase64String;
    }
}