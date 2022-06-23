using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Helpers;

public class Aes256Cipher
{
    private readonly byte[] _key;

    public Aes256Cipher(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new NullReferenceException("The key is empty");
        _key = Convert.FromBase64String(key);
    }

    public string Decrypt(string value)
    {
        var ivAndCipherText = Convert.FromBase64String(value);
        using var aes = Aes.Create();
        var ivLength = aes.BlockSize / 8;
        aes.IV = ivAndCipherText.Take(ivLength).ToArray();
        aes.Key = _key;
        using var cipher = aes.CreateDecryptor();
        var cipherText = ivAndCipherText.Skip(ivLength).ToArray();
        var text = cipher.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(text);
    }
}