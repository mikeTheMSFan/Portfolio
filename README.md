# Portfolio project

This shows the code that I used on my website.

## Running the program

1. Download [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/) or higher and install it, along with .NET 6.

2. After you install Visual Studio, go to the root of the project and double click the `Portfolio.sln` file.

3. You will need to use Visual Studio to build the project, to get familiar with building projects in Visual Studio please see [this article](https://docs.microsoft.com/en-us/visualstudio/ide/walkthrough-building-an-application?view=vs-2022).

4. Before you can run the software, create your own `appsettings.json` file; if you need guidance creating this file, please see the template below.

```json
{
  "ConnectionStrings": {
    "Production": "server=localhost;userid=root;pwd=yourpassword;port=3306;database=portfolio"
  },
  "AzureAd": {
    "CallBackPath": "/signin-oidc",
    "ClientCertificates": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "Your keyvault url",
        "KeyVaultCertificateName": "Your certificate name"
      }
    ],
    "ClientId": "Your client id",
    "Instance": "https://login.microsoftonline.com/",
    "KeyVaultCertificateName": "Your cetificate name",
    "KeyVaultUrl": "Your keyvault url",
    "TenantId": "common"
  },
  "GoogleAccount": {
    "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
    "auth_uri": "https://accounts.google.com/o/oauth2/auth",
    "client_id": "Your Google client id",
    "client_secret": "Your Google client secret",
    "project_id": "Your project id",
    "redirect_uris": ["Your redirect URIs"],
    "token_uri": "https://oauth2.googleapis.com/token"
  },
  "MailSettings": {
    "DisplayName": "Your display name",
    "Host": "Your smtp server (StartTLS)",
    "Mail": "Your email address",
    "Password": "Your password",
    "Port": 587
  },
  "SftpSettings": {
    "BlogUploadDirectory": "Your blog picture directory",
    "Host": "Your content host",
    "PassPhrase": "Your passphrase",
    "Port": 22,
    "PostUploadDirectory": "Your post picture directory",
    "ProfileUploadDirectory": "Your profile picture directory",
    "ProjectUploadDirectory": "Your project picture directory",
    "StorageUrl": "https://mycontentserver.com/",
    "UserName": "Your username"
  }
}
```

## Configuration

### **Setup SSH for image storage**

This project uses [SSH.NET](https://github.com/sshnet/SSH.NET/) to upload pictures to a content server. To set up ssh:

1. Under `SftpSettings`, put the directory where you would like to store your blog pictures in the `BlogUploadDirectory` portion of `appsettings.json`.

2. Under `SftpSettings`, put the directory where you would like to store your post pictures in the `PostUploadDirectory` portion of `appsettings.json`.

3. Under `SftpSettings`, put the directory where you would like to store your profile pictures in the `ProfileUploadDirectory` portion of `appsettings.json`.

4. Under `SftpSettings`, put the directory where you would like to store your profile pictures in the `ProjectUploadDirectory` portion of `appsettings.json`.

5. Under `SftpSettings`, put your ssh username in the `UserName` portion of `appsettings.json`.

6. Under `SftpSettings`, put your ssh pass phrase in the `PassPhrase` portion of `appsettings.json`.

7. Under `SftpSettings`, put your ssh host in the `Host` portion of `appsettings.json`.

8. Under `SftpSettings`, put your ssh port in the `Port` portion of `appsettings.json`.

9. Under `SftpSettings`, put your storeage url in the `StorageUrl` portion of `appsettings.json`.

### **Set up Microsoft login option**

When the user logs in using their Microsoft account leveraging Microsoft’s Identity platform, you can save an access token. This token is used to authenticate against Microsoft’s Graph API and, if available, pull a great deal of user information. To set up the Microsoft Login option:

1.  You need to sign up for an Azure account which will give you one instance of Azure AD, within Azure Ad you need to set up an [app registration](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app). Once this is setup you will have your `ClientId`, as well as a host of other information. Please see the app registration link for more information on how to set up your redirect URI, and app secrets or certificates (which ever you may choose).

2.  Within your app registration, you need to set up Microsoft Graph. To do this:

    1. Click on `API permissions`.

    2. In the resulting window, click on `Add a permission`.

    3. In the blade that opens from the right, click on `Microsoft Graph`.

    4. The blade will refresh asking you what permission type you would like to set up, choose `Delegated permissions`. These are the permissions I chose when creating the app (You may need other permissions depending on how you wish to move forward):

       1. email - Allows the app to read your primary email address.

       2. openid - Allows you to sign in to the app with your work or school account and allows the app to read your basic profile information.

       3. User.Read - Allows you to sign in to the app with your organizational account and let the app read your profile. It also allows the app to read basic company information.

3.  Input values into `appsettings.json`.

    1. Under `AzureAd`, put the client id in `ClientId` portion of `appsettings.json`.

    2. Under `AzureAd`, put the key vault URL in `KeyVaultUrl` portions of `appsettings.json`.

    3. Under `AzureAd`, put the key vault certificate name in `KeyVaultCertificateName` portions of `appsettings.json`.

    If you would rather use a client secrect (which is usually for development) you can follow this template:

    ```json
    {
      "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": "[Enter the tenantId here]",
        "ClientId": "[Enter the Client Id]",
        "CallbackPath": "/signin-oidc"
      }
    }
    ```

### **Set up the Google login option**

Microsoft has done a lot of the legwork for setting up Google account authentication, and, unlike Microsoft, we do not need to use any api to get the information we need. They provide all of the information in a JWT Token. This token provides all relevant information about the user, including a profile picture if available. To set up the Google sign-in option:

1. Create Google client id and client secret.

2. Under `GoogleAccount`, put your client in the `client_id` portion of `appsettings.json`.

3. Under `GoogleAccount`, put your client secret in the `client_secret` portion of `appsettings.json`.

For information on how to get these values,please review the [`Create the Google OAuth 2.0 Client ID and secret`](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-6.0) and [`Store the Google client ID and secret`](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-6.0#store-the-google-client-id-and-secret)

### **Set up the email sender**

The email sender will do things like verify user emails and send you messages from the contact page. It will send all emails required by the app.
To set up the email senter:

1. Under `MailSettings`, put your name in the `DisplayName` portion of `appsettings.json`.

2. Under `MailSettings`, put your smtp mail host in the `Host` portion of `appsettings.json`.

3. Under `MailSettings`, put your reply email address in the `Mail` portion of `appsettings.json`.

4. Under `MailSettings`, put your password in the `Password` portion of `appsettings.json`.

5. Under `MailSettings`, put your mail host port in the `Port` portion of `appsettings.json`.

If you could report any bugs or vulnerabilities to me, I would appreciate it. Thank you for taking the time to read through this. :)
