﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CacheExtensionsTests
    {
        [Fact]
        public void InMemoryCacheExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddInMemoryTokenCache();

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public async Task CacheExtensions_CcaAlreadyExists_TestsAsync()
        {
            AuthenticationResult result;
            // new InMemory serializer and new cca
            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, addInstanceMock: true).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // new DistributedInMemory and same cca
            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        [Fact]
        public void InMemoryCacheExtensions_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache());

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_ExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            });

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoService_ThrowsException_Tests()
        {
            var confidentialApp = CreateCca();
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache(null!));

            Assert.Equal("initializeMemoryCache", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            });

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public void DistributedCacheExtensions_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensions_NoService_ThrowsException_Tests()
        {
            var confidentialApp = CreateCca();
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddDistributedTokenCache(null!));

            Assert.Equal("initializeDistributedCache", ex.ParamName);
        }

        [Fact]
        public async Task SingletonMsal_ResultsInCorrectCacheEntries_Test()
        {
            MockHttpClientFactory mockHttpClient = new MockHttpClientFactory();
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateInstanceDiscoveryMockHandler());
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttpClient)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

            var distributedCache = new TestDistributedCache();
            confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddSingleton<IDistributedCache>(distributedCache);
            });

            // Different tenants used to created different cache entries
            var result1 = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                .WithTenantId("tenant1")
                .ExecuteAsync().ConfigureAwait(false);
            var result2 = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                .WithTenantId("tenant2")
                .ExecuteAsync().ConfigureAwait(false);

            Assert.Equal(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(2, distributedCache._dict.Count);
            Assert.Equal(distributedCache.Get($"{TestConstants.ClientId}_tenant1_AppTokenCache")!.Length, distributedCache.Get($"{TestConstants.ClientId}_tenant2_AppTokenCache")!.Length);
        }

        private enum CacheType
        {
            InMemory,
            DistributedInMemory,
        }

        private static async Task<AuthenticationResult> CreateAppAndGetTokenAsync(
            CacheType cacheType,
            bool addTokenMock = true,
            bool addInstanceMock = false)
        {
            using MockHttpClientFactory mockHttp = new MockHttpClientFactory();
            using var discoveryHandler = MockHttpCreator.CreateInstanceDiscoveryMockHandler();
            using var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            if (addInstanceMock)
            {
                mockHttp.AddMockHandler(discoveryHandler);
            }

            // for when the token is requested from ESTS
            if (addTokenMock)
            {
                mockHttp.AddMockHandler(tokenHandler);
            }

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttp)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

            switch (cacheType)
            {
                case CacheType.InMemory:
                    confidentialApp.AddInMemoryTokenCache();
                    break;

                case CacheType.DistributedInMemory:
                    confidentialApp.AddDistributedTokenCache(services =>
                    {
                        services.AddDistributedMemoryCache();
                        services.AddLogging(configure => configure.AddConsole())
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Warning);
                    });
                    break;
            }

            var result = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                .ExecuteAsync().ConfigureAwait(false);

            return result;
        }

        private IConfidentialClientApplication CreateCca() =>
                        ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();
    }
}
