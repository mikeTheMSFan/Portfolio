using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Portfolio.Extensions;
using Portfolio.Services.Interfaces;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;

namespace Portfolio.Services;

public class BasicExternalLoginService : IExternalLogin
{
    public async Task<string> GetMicrosoftGraphPhotoAsync(string token)
    {
        //set configuration
        var configuration = GetConfiguration();
        var clientId = configuration.GetSection("AzureAd").GetSection("ClientId").Value;
        var vaultUrl = configuration.GetSection("AzureAd").GetSection("KeyVaultUrl").Value;
        var certName = configuration.GetSection("AzureAd").GetSection("KeyVaultCertificateName").Value;
        var tenantId = configuration.GetSection("AzureAd").GetSection("TenantId").Value;

        //set up certificate client.
        var client = new CertificateClient(new Uri(vaultUrl), new DefaultAzureCredential());

        //get appropriate certificate
        var cert = await client.DownloadCertificateAsync(certName);

        //create cca using configuration and certificate
        var cca = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithTenantId(tenantId)
            .WithCertificate(cert.Value)
            .Build();

        //define the applicable scope.
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        //create authentication provider to use with Microsoft Graph.
        var authProvider = new DelegateAuthenticationProvider(async request =>
        {
            var assertion = new UserAssertion(token);
            var result = await cca.AcquireTokenOnBehalfOf(scopes, assertion).ExecuteAsync();

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", result.AccessToken);
        });

        //get new instance of Graph Service Client.
        var graphClient = new GraphServiceClient(authProvider);

        //use Graph client to get stream of photo.
        var stream = await graphClient.Me.Photos["120x120"]
            .Content
            .Request()
            .GetAsync();

        //if the stream is null, there is no picture.
        if (stream == null) return null!;

        //create image from stream
        var image = Image.Load(stream, out var format);

        //return base64 image.
        return image.ToBase64String(format);
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