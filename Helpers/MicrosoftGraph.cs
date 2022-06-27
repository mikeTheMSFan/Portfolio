using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Portfolio.Extensions;

namespace Portfolio.Helpers;

public static class MicrosoftGraph
{
    public static async Task<GraphServiceClient> GetMicrosoftGraphSingleUserClient (string token)
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

    private static IConfigurationRoot GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()
            .Decrypt("CipherKey", "CipherText:");

        return configuration;
    }
}