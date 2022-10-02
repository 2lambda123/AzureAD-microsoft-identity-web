﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider based on ITokenAcquisition.
    /// </summary>
    internal class TokenAcquisitionAuthenticationProvider : IAuthenticationProvider
    {
        public TokenAcquisitionAuthenticationProvider(IAuthorizationHeaderProvider authorizationHeaderProvider, TokenAcquisitionAuthenticationProviderOption options)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _initialOptions = options;
        }

        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly TokenAcquisitionAuthenticationProviderOption _initialOptions;

        /// <summary>
        /// Adds an authorization header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // Default options to settings provided during intialization
            var scopes = _initialOptions.Scopes;
            bool appOnly = _initialOptions.AppOnly ?? false;
            string? tenant = _initialOptions.Tenant ?? null;
            string? scheme = _initialOptions.AuthenticationScheme ?? null;
            // Extract per-request options from the request if present
            TokenAcquisitionAuthenticationProviderOption? msalAuthProviderOption = GetMsalAuthProviderOption(request);
            if (msalAuthProviderOption != null) {
                scopes = msalAuthProviderOption.Scopes ?? scopes;
                appOnly = msalAuthProviderOption.AppOnly ?? appOnly;
                tenant = msalAuthProviderOption.Tenant ?? tenant;
                scheme = msalAuthProviderOption.AuthenticationScheme ?? scheme;
            }

            if (!appOnly && scopes == null)
            {
                throw new InvalidOperationException(IDWebErrorMessage.ScopesRequiredToCallMicrosoftGraph);
            }

            DownstreamRestApiOptions? downstreamRestApiOptions = new DownstreamRestApiOptions() { BaseUrl = "https://graph.microsoft.com", Scopes = scopes };
            downstreamRestApiOptions.TokenAcquirerOptions.AuthenticationOptionsName = scheme;
            downstreamRestApiOptions.TokenAcquirerOptions.Tenant = tenant;

            if (msalAuthProviderOption?.DownstreamRestApiOptions != null)
            {
                msalAuthProviderOption.DownstreamRestApiOptions(downstreamRestApiOptions);
            }

            string authorizationHeader;
            if (appOnly)
            {
                authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                    Constants.DefaultGraphScope,
                    downstreamRestApiOptions).ConfigureAwait(false);
            }
            else
            {
                authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    scopes!,
                    downstreamRestApiOptions).ConfigureAwait(false);
            }

            // add or replace authorization header
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            request.Headers.Add(
                Constants.Authorization, authorizationHeader);

            downstreamRestApiOptions?.CustomizeHttpRequestMessage?.Invoke(request);
        }

        /// <summary>
        /// Extract TokenAcquisitionAuthenticationProviderOption from request.Properties if it is present
        /// </summary>
        /// <param name="httpRequestMessage">Current request message</param>
        /// <returns>Options set for just this request.</returns>
        private TokenAcquisitionAuthenticationProviderOption? GetMsalAuthProviderOption(HttpRequestMessage httpRequestMessage)
        {
            AuthenticationHandlerOption authHandlerOption = httpRequestMessage.GetMiddlewareOption<AuthenticationHandlerOption>();

            return authHandlerOption?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption;
        }
    }
}
