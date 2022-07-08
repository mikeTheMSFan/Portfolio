using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Portfolio.Services.Interfaces;
using Portfolio.Models.Settings;
using SixLabors.ImageSharp;

namespace Portfolio.Services;

public class MicrosoftExternalLogin: IExternalLoginService
{
    private readonly AppSettings _appSettings;
    public MicrosoftExternalLogin(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }
    
    public async Task<string> GetBase64ExternalLoginPictureAsync([Optional]string token)
    {
        var singleUserGraphClient = await GetMicrosoftGraphSingleUserClient(token);
                    
        //use Graph client to get stream of photo.
        var stream = await singleUserGraphClient.Me.Photos["120x120"]
            .Content
            .Request()
            .GetAsync();

        if (stream == null) return string.Empty;
                    
        //create image from stream
        var image = SixLabors.ImageSharp.Image.Load(stream, out var format);
        return image.ToBase64String(format);
    }
    
    private async Task<GraphServiceClient> GetMicrosoftGraphSingleUserClient (string token)
    {
        //set up certificate client.
        var client = new CertificateClient(new Uri(_appSettings.AzureAd.ClientCertificates.FirstOrDefault()!.KeyVaultUrl), new DefaultAzureCredential());

        //get appropriate certificate
        var cert = await client.DownloadCertificateAsync(_appSettings.AzureAd.ClientCertificates.FirstOrDefault()!.KeyVaultCertificateName);

        //create cca using configuration and certificate
        var cca = ConfidentialClientApplicationBuilder
            .Create(_appSettings.AzureAd.ClientId)
            .WithTenantId(_appSettings.AzureAd.TenantId)
            .WithCertificate(cert.Value)
            .Build();

        //create authentication provider to use with Microsoft Graph.
        var authProvider = new DelegateAuthenticationProvider(async request =>
        {
            var scopes = new[] { "user.read" };
            var assertion = new UserAssertion(token);
            var result = await cca.AcquireTokenOnBehalfOf(scopes, assertion).ExecuteAsync();

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", result.AccessToken);
        });

        //get new instance of Graph Service Client.
        var graphClient = new GraphServiceClient(authProvider);

        return graphClient;
    }
}