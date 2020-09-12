﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension for IServiceCollection for startup initialization.
    /// </summary>
    public static partial class MicrosoftIdentityWebAppServiceCollectionExtensions
    {
        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, (by default named "AzureAd"), with the necessary settings to
        /// initialize the authentication options.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">The IConfiguration object.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <param name="cookieScheme">Optional name for the cookie authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme).</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The authentication builder to chain extension methods.</returns>
        [Obsolete("Rather use AddMicrosoftIdentityWebAppAuthentication(). See https://aka.ms/ms-id-web/0.3.0-preview")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration AddMicrosoftWebAppAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = IDWebConstants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(openIdConnectScheme);
            return builder.AddMicrosoftIdentityWebApp(
                configuration,
                configSectionName,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);
        }
    }
}
