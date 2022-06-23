using System.Text;
using System.Text.RegularExpressions;
using NsfwSpyNS;
using SixLabors.ImageSharp;

namespace Portfolio.Extensions;

public static class FormFileExtensions
{
    public const int ImageMinimumBytes = 512;

    public static bool IsImage(this IFormFile postedFile)
    {
        //-------------------------------------------
        //  Check the image mime types
        //-------------------------------------------
        if (!string.Equals(postedFile.ContentType, "image/jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/x-png", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/png", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/svg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(postedFile.ContentType, "image/*", StringComparison.OrdinalIgnoreCase))
            return false;

        //-------------------------------------------
        //  Check the image extension
        //-------------------------------------------
        var postedFileExtension = Path.GetExtension(postedFile.FileName);
        if (!string.Equals(postedFileExtension, ".jpg", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(postedFileExtension, ".png", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(postedFileExtension, ".gif", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(postedFileExtension, ".jpeg", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(postedFileExtension, ".svg", StringComparison.OrdinalIgnoreCase))
            return false;

        //-------------------------------------------
        //  Try load file as image, if .NET will throw exception
        //  we can assume that it's not a valid image
        //  (updated to use Imagesharp)
        //-------------------------------------------
        try
        {
            var image = Image.Load(postedFile.OpenReadStream());
        }
        catch (Exception)
        {
            return false;
        }

        //-------------------------------------------
        //  Attempt to read the file and check the first bytes
        //-------------------------------------------
        try
        {
            if (postedFile.Length < 0) return false;
            //------------------------------------------
            //   Check whether the image size exceeding the limit or not
            //------------------------------------------ 
            if (postedFile.Length < ImageMinimumBytes) return false;

            using var newMs = new MemoryStream();
            postedFile.CopyTo(newMs);
            var fileInBytes = newMs.ToArray();

            var buffer = new byte[fileInBytes.Length];
            Array.Copy(fileInBytes, buffer, fileInBytes.Length);
            var content = Encoding.UTF8.GetString(buffer);
            if (Regex.IsMatch(content,
                    @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                return false;
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public static bool ImageSizeIsCorrect(this IFormFile postedFile)
    {
        try
        {
            var image = Image.Load(postedFile.OpenReadStream());
            var w = image.Width;
            var h = image.Height;
            var aspectRatio = 21 / (decimal)9;

            var r = CheckRatio(w, h);

            if (r != aspectRatio) return false;
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public static bool IsDecent(this IFormFile postedFile)
    {
        var nsfwSpy = new NsfwSpy();
        if (postedFile.IsImage())
        {
            using var fs = postedFile.OpenReadStream();
            var bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);

            var result = nsfwSpy.ClassifyImage(bytes);
            if (result.PredictedLabel != "Neutral") return false;
        }

        return true;
    }

    //If the image is or is close enough to (21/9), allow it.
    private static decimal CheckRatio(int a, int b)
    {
        var ratio = a / (decimal)b;
        var target = 21 / (decimal)9;
        if (ratio == target) return ratio;

        var marginOfError = (decimal)0.02;
        if (Math.Abs(target - ratio) <= marginOfError) return target;
        return 1;
    }
}