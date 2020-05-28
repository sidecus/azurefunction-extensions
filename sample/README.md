# azurefunction extension for user secrets and Azure Key Vault
Opinionated .Net core Azure Function extension with user secrets and Azure Key Vault support.

## Motivation
I started looking at Azure Function a few days ago and noticed a few issues:
1. For local development, Azure Function uses local.settings.json for all local settings including secrets. This means you have to put local app settings in that file but cannot check it in since it might contain secrets even though they are for local development purpose. If you have multiple team members working on the same Function project this can be a bit tricky since their local copy of local.settings.json can get outdated soon.
2. In production you'll likely want to use Azure Key Vault to store your secrets. Right now there are two ways to load secrets in Azure function:
    - Manage your own Key Vault client and invoke it when you are loading secrets.
    - Use [Key Vault references](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references) by which secrets are automatically loaded for you.

Both these are viable options but there are take backs with them as well. First option means you have to write "imperative" code for secret loading (instead of config binding), and the latter means you'll have to repeat each secret in app settings.

I came from asp.net core world and I really like what has been done there for secret management. If you are not familiar with it, you might want to check these asp.net core concepts out: [user secrets configuration provider](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows) and [azure key vault configuration provider](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1).

Furthermore, .net core Azure Function now supports [Dependency Injection](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection). If we can do something similar in Azure Function for secret management as what asp.net core does, we can then use the [IOptions pattern](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection) to simplify our code a lot.

## Opinionated solution
This is why I created this extension. It can potentially help you with a few things:
1. By default you can manage your development environment secrets in [asp.net core user secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows).
2. If you are like me and don't want to put all local secerts in user secrets file and get every developer in the team to maintain a local copy (it can get outdated easily), you can set up a development specific Azure Key Vault and save dev secrets there (with app id and secret authentication). To enable this you'll need to define ```KeyVaultName``` in local.settings.json (remember you can now check it in to your repo), and put just ```KeyVaultAppId``` and ```KeyVaultAppSecret``` into asp.net core user secrets.
3. For production environments Azure Key Vault is turned on automatically with managed identity if ```KeyVaultName``` is defined in app settings. It's by intention that app id and app secret based authentication is not supported in this extension since [managed identity](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet) is much more secure.
4. All secrets can be accessed via the IOptions pattern. [```AppConfig```](https://github.com/sidecus/azurefunction_keyvault/blob/master/AppConfig.cs) for example contains an ```AppSecret``` field which is automatically bound to either user secrets or Key Vault.

All these can be done with just few lines of code in the ```Startup.Configure```:
```
// Add azure keyvault
builder.AddSecrets();

// Inject IOptions pattern for AppConfig (which can reference user secrets or KeyVault secrets)
builder.Services
    .AddOptions<AppConfig>()
    .Configure<IConfiguration>((appConfig, configuration) =>
    {
        configuration.GetSection("AppConfig").Bind(appConfig);
    });

```

Kindly note I used some tricks here since the Azure Function SDK doesn't provide the capability to customize configuration builder. All code is in [```FunctionHostBuilderSecretExtensions```](https://github.com/sidecus/azurefunction_keyvault/blob/master/HostBuilderAzureKeyVaultExtension.cs).
I also used ```ActionResult<T>``` instead of ```IActionResult``` as function return type since it provides much better result type checking.

## Disclaimer
* Code in ```MyHttpTrigger``` is *for demo purpose only*. You should never dump your secret or settings in this way for real production usage.
* Secrets are not automatically refreshed now. If you need that capability you can build on top of this.
* Azure Function runtime depends on a lot of environment variables which are also injected as part of IConfiguration. Since we are extending it with KeyVault secrets there is possibility of "name" collision which can impact the run time behavior. Please make sure you use unique naming patterns for your secrets to avoid this.

**Enjoy and happy coding. Peace.**