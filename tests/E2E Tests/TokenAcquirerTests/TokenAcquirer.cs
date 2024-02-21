// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace TokenAcquirerTests
{
#if !FROM_GITHUB_ACTION
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class TokenAcquirer
    {
        private static readonly string s_optionName = string.Empty;
        private static readonly CredentialDescription[] s_clientCredentials = new[]
        {
            CertificateDescription.FromKeyVault(
                "https://webappsapistests.vault.azure.net",
                "Self-Signed-5-5-22")
        };

        public TokenAcquirer()
        {
            TokenAcquirerFactory.ResetDefaultInstance(); // Test only
        }

        [Fact]
        public void TokenAcquirerFactoryDoesNotUseAspNetCoreHost()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            var serviceProvider = tokenAcquirerFactory.Build();
            var service = serviceProvider.GetService<ITokenAcquisitionHost>();
            Assert.NotNull(service);
            Assert.Equal("Microsoft.Identity.Web.Hosts.DefaultTokenAcquisitionHost", service.GetType().FullName);
        }

        [IgnoreOnAzureDevopsFact]
        //[Theory]
        //[InlineData(false)]
        //[InlineData(true)]
        public async Task AcquireToken_WithMicrosoftIdentityOptions_ClientCredentialsAsync(/*bool withClientCredentials*/)
        {
            bool withClientCredentials = false; //add as param above
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                if (withClientCredentials)
                {
                    option.ClientCertificates = s_clientCredentials.OfType<CertificateDescription>();
                }
                else
                {
                    option.ClientCredentials = s_clientCredentials;
                }
            });

            await CreateGraphClientAndAssert(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireToken_WithMicrosoftIdentityApplicationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            await CreateGraphClientAndAssert(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndMicrosoftIdentityApplicationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            // Get the token acquirer from the options.
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
            {
                ClientId = "6af093f3-b445-4b7a-beae-046864468ad6",
                Authority = "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                ClientCredentials = s_clientCredentials
            });

            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndAuthorityClientIdCert_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(
                authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
                clientCredentials: s_clientCredentials);

            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task LoadCredentialsIfNeededAsync_MultipleThreads_WaitsForSemaphore()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();

            var task1 = Task.Run(async () =>
            {
                await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            });

            var task2 = Task.Run(async () =>
            {
                await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            });

            // Run task1 and task2 concurrently
            await Task.WhenAll(task1, task2);

            var cert = options.ClientCredentials!.First().Certificate;

            Assert.NotNull(cert);
            Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithPop_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();
            await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            var cert = options.ClientCredentials!.First().Certificate;

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                   new TokenAcquisitionOptions() { PopPublicKey = ComputePublicKeyString(cert) });
            Assert.NotNull(result.AccessToken);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithMs10AtPop_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();
            await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            var cert = options.ClientCredentials!.First().Certificate;

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            RsaSecurityKey rsaSecurityKey = CreateRsaSecurityKey();
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                   new TokenAcquisitionOptions()
                   {
                       PopPublicKey = rsaSecurityKey.KeyId,
                       PopClaim = CreatePopClaim(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
                   });
            Assert.NotNull(result.AccessToken);
        }

        private static string CreatePopClaim(RsaSecurityKey key, string algorithm)
        {
            var parameters = key.Rsa == null ? key.Parameters : key.Rsa.ExportParameters(false);
            return "{\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(parameters.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(parameters.Exponent) + "\",\"alg\":\"" + algorithm + "\",\"kid\":\"" + key.KeyId + "\"}";
        }

        private static RsaSecurityKey CreateRsaSecurityKey()
        {
#if NET472
            RSA rsa = RSA.Create(2048);
#else
            RSA rsa = new RSACryptoServiceProvider(2048);
#endif
            // the reason for creating the RsaSecurityKey from RSAParameters is so that a SignatureProvider created with this key
            // will own the RSA object and dispose it. If we pass a RSA object, the SignatureProvider does not own the object, the RSA object will not be disposed.
            RSAParameters rsaParameters = rsa.ExportParameters(true);
            RsaSecurityKey rsaSecuirtyKey = new(rsaParameters) { KeyId = CreateRsaKeyId(rsaParameters) };
            rsa.Dispose();
            return rsaSecuirtyKey;
        }

        private static string CreateRsaKeyId(RSAParameters rsaParameters)
        {
            Throws.IfNull(rsaParameters.Exponent);
            Throws.IfNull(rsaParameters.Modulus);

            byte[] kidBytes = new byte[rsaParameters.Exponent.Length + rsaParameters.Modulus.Length];
            Array.Copy(rsaParameters.Exponent, 0, kidBytes, 0, rsaParameters.Exponent.Length);
            Array.Copy(rsaParameters.Modulus, 0, kidBytes, rsaParameters.Exponent.Length, rsaParameters.Modulus.Length);
            using (var sha2 = SHA256.Create())
            {
                return Base64UrlEncoder.Encode(sha2.ComputeHash(kidBytes));
            }
        }

        private string? ComputePublicKeyString(X509Certificate2? certificate)
        {
            if (certificate == null)
            {
                return null;
            }
            // We Create the Pop public key
            var key = new X509SecurityKey(certificate);
            string base64EncodedJwk = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
            var reqCnf = $@"{{""kid"":""{base64EncodedJwk}""}}";
            // 1.4. Base64 encode it again
            var keyId = Base64UrlEncoder.Encode(reqCnf);
            return keyId;
        }

        private static async Task CreateGraphClientAndAssert(TokenAcquirerFactory tokenAcquirerFactory, IServiceCollection services)
        {
            services.AddInMemoryTokenCaches();
            services.AddMicrosoftGraph();
            var serviceProvider = tokenAcquirerFactory.Build();
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .GetAsync(o => o.Options
                                 .WithAppOnly()
                                 .WithAuthenticationScheme(s_optionName));
            Assert.True(users!=null && users.Value!=null && users.Value.Count >= 56);

            // Alternatively to calling Microsoft Graph, you can get a token acquirer service
            // and get a token, and use it in an SDK.
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.NotNull(result.AccessToken);
        }
    }

    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class AcquireTokenManagedIdentity 
    {
        [OnlyOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithManagedIdentity_UserAssigned()
        {
            // Arrange
            const string scope = "https://vault.azure.net/.default";
            const string baseUrl = "https://vault.azure.net";
            const string clientId = "9c5896db-a74a-4b1a-a259-74c5080a3a6a";
            TokenAcquirerFactory.ResetDefaultInstance();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();

            // Act: Get the authorization header provider and add the options to tell it to use Managed Identity
            IAuthorizationHeaderProvider? api = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.NotNull(api);
            string result = await api.CreateAuthorizationHeaderForAppAsync(scope, GetAuthHeaderOptions_ManagedId(baseUrl, clientId));

            // Assert: Make sure we got a token
            Assert.False(string.IsNullOrEmpty(result));
        }

        private static AuthorizationHeaderProviderOptions GetAuthHeaderOptions_ManagedId(string baseUrl, string? userAssignedClientId=null) 
        {
            ManagedIdentityOptions managedIdentityOptions = new()
            {
                UserAssignedClientId = userAssignedClientId
            };
            AcquireTokenOptions aquireTokenOptions = new()
            {
                ManagedIdentity = managedIdentityOptions
            };
            return new AuthorizationHeaderProviderOptions()
            {
                BaseUrl = baseUrl,
                AcquireTokenOptions = aquireTokenOptions
            };
        }
    }
#endif //FROM_GITHUB_ACTION
}
