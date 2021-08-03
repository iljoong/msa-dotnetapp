using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace apiapp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-5.0
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // user-assigned managed identity is not working with AppSvc, AKS(?)
                    var use_kv = (System.Environment.GetEnvironmentVariable("USE_KV") ?? "false").ToLower() == "true";
                    if (context.HostingEnvironment.IsProduction() && use_kv)
                    {
                        Console.WriteLine($"Using KV configuration");

                        var builtConfig = config.Build();
                        
                        var _kvname = builtConfig["keyvault:name"];
                        var kvacct = System.Environment.GetEnvironmentVariable("KV_NAME") ?? _kvname;

                        var secretClient = new SecretClient(
                            new Uri($"https://{kvacct}.vault.azure.net/"),
                            new DefaultAzureCredential());
                        config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    string port = System.Environment.GetEnvironmentVariable("APP_PORT") ?? "80";
                    Console.WriteLine($"port={port}");
                    if (port != null && port != "")
                        webBuilder.UseUrls($"http://*:{port}");

                    webBuilder.UseStartup<Startup>();
                });
    }
}
