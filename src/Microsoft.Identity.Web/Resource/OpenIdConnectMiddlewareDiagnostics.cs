﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Diagnostics used in the OpenID Connect middleware
    /// (used in web Apps).
    /// </summary>
    public class OpenIdConnectMiddlewareDiagnostics : IOpenIdConnectMiddlewareDiagnostics
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor of the <see cref="OpenIdConnectMiddlewareDiagnostics"/>, used
        /// by dependency injection.
        /// </summary>
        /// <param name="logger">Logger used to log the diagnostics.</param>
        public OpenIdConnectMiddlewareDiagnostics(ILogger<OpenIdConnectMiddlewareDiagnostics> logger)
        {
            _logger = logger;
        }

        // Summary:
        //     Invoked before redirecting to the identity provider to authenticate. This can
        //     be used to set ProtocolMessage.State that will be persisted through the authentication
        //     process. The ProtocolMessage can also be used to add or customize parameters
        //     sent to the identity provider.
        private Func<RedirectContext, Task> s_onRedirectToIdentityProvider = null!;

        // Summary:
        //     Invoked when a protocol message is first received.
        private Func<MessageReceivedContext, Task> s_onMessageReceived = null!;

        // Summary:
        //     Invoked after security token validation if an authorization code is present in
        //     the protocol message.
        private Func<AuthorizationCodeReceivedContext, Task> s_onAuthorizationCodeReceived = null!;

        // Summary:
        //     Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        private Func<TokenResponseReceivedContext, Task> s_onTokenResponseReceived = null!;

        // Summary:
        //     Invoked when an IdToken has been validated and produced an AuthenticationTicket.
        private Func<TokenValidatedContext, Task> s_onTokenValidated = null!;

        // Summary:
        //     Invoked when user information is retrieved from the UserInfoEndpoint.
        private Func<UserInformationReceivedContext, Task> s_onUserInformationReceived = null!;

        // Summary:
        //     Invoked if exceptions are thrown during request processing. The exceptions will
        //     be re-thrown after this event unless suppressed.
        private Func<AuthenticationFailedContext, Task> s_onAuthenticationFailed = null!;

        // Summary:
        //     Invoked when a request is received on the RemoteSignOutPath.
        private Func<RemoteSignOutContext, Task> s_onRemoteSignOut = null!;

        // Summary:
        //     Invoked before redirecting to the identity provider to sign out.
        private Func<RedirectContext, Task> s_onRedirectToIdentityProviderForSignOut = null!;

        // Summary:
        //     Invoked before redirecting to the Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions.SignedOutRedirectUri
        //     at the end of a remote sign-out flow.
        private Func<RemoteSignOutContext, Task> s_onSignedOutCallbackRedirect = null!;

        /// <summary>
        /// Subscribes to all the OpenIdConnect events, to help debugging, while
        /// preserving the previous handlers (which are called).
        /// </summary>
        /// <param name="events">Events to subscribe to.</param>
        public void Subscribe(OpenIdConnectEvents events)
        {
            events ??= new OpenIdConnectEvents();

            s_onRedirectToIdentityProvider = events.OnRedirectToIdentityProvider;
            events.OnRedirectToIdentityProvider = OnRedirectToIdentityProviderAsync;

            s_onMessageReceived = events.OnMessageReceived;
            events.OnMessageReceived = OnMessageReceivedAsync;

            s_onAuthorizationCodeReceived = events.OnAuthorizationCodeReceived;
            events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync;

            s_onTokenResponseReceived = events.OnTokenResponseReceived;
            events.OnTokenResponseReceived = OnTokenResponseReceivedAsync;

            s_onTokenValidated = events.OnTokenValidated;
            events.OnTokenValidated = OnTokenValidatedAsync;

            s_onUserInformationReceived = events.OnUserInformationReceived;
            events.OnUserInformationReceived = OnUserInformationReceivedAsync;

            s_onAuthenticationFailed = events.OnAuthenticationFailed;
            events.OnAuthenticationFailed = OnAuthenticationFailedAsync;

            s_onRemoteSignOut = events.OnRemoteSignOut;
            events.OnRemoteSignOut = OnRemoteSignOutAsync;

            s_onRedirectToIdentityProviderForSignOut = events.OnRedirectToIdentityProviderForSignOut;
            events.OnRedirectToIdentityProviderForSignOut = OnRedirectToIdentityProviderForSignOutAsync;

            s_onSignedOutCallbackRedirect = events.OnSignedOutCallbackRedirect;
            events.OnSignedOutCallbackRedirect = OnSignedOutCallbackRedirectAsync;
        }

        private async Task OnRedirectToIdentityProviderAsync(RedirectContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRedirectToIdentityProviderAsync)));

            await s_onRedirectToIdentityProvider(context).ConfigureAwait(false);

            _logger.LogDebug("   Sending OpenIdConnect message:");
            DisplayProtocolMessage(context.ProtocolMessage);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRedirectToIdentityProviderAsync)));
        }

        private void DisplayProtocolMessage(OpenIdConnectMessage message)
        {
            foreach (var property in message.GetType().GetProperties())
            {
                object? value = property.GetValue(message);
                if (value != null)
                {
                    _logger.LogDebug($"   - {property.Name}={value}");
                }
            }
        }

        private async Task OnMessageReceivedAsync(MessageReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnMessageReceivedAsync)));
            _logger.LogDebug("   Received from STS the OpenIdConnect message:");
            DisplayProtocolMessage(context.ProtocolMessage);
            await s_onMessageReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnMessageReceivedAsync)));
        }

        private async Task OnAuthorizationCodeReceivedAsync(AuthorizationCodeReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnAuthorizationCodeReceivedAsync)));
            await s_onAuthorizationCodeReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnAuthorizationCodeReceivedAsync)));
        }

        private async Task OnTokenResponseReceivedAsync(TokenResponseReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnTokenResponseReceivedAsync)));
            await s_onTokenResponseReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnTokenResponseReceivedAsync)));
        }

        private async Task OnTokenValidatedAsync(TokenValidatedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnTokenValidatedAsync)));
            await s_onTokenValidated(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnTokenValidatedAsync)));
        }

        private async Task OnUserInformationReceivedAsync(UserInformationReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnUserInformationReceivedAsync)));
            await s_onUserInformationReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnUserInformationReceivedAsync)));
        }

        private async Task OnAuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnAuthenticationFailedAsync)));
            await s_onAuthenticationFailed(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnAuthenticationFailedAsync)));
        }

        private async Task OnRedirectToIdentityProviderForSignOutAsync(RedirectContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRedirectToIdentityProviderForSignOutAsync)));
            await s_onRedirectToIdentityProviderForSignOut(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRedirectToIdentityProviderForSignOutAsync)));
        }

        private async Task OnRemoteSignOutAsync(RemoteSignOutContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRemoteSignOutAsync)));
            await s_onRemoteSignOut(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRemoteSignOutAsync)));
        }

        private async Task OnSignedOutCallbackRedirectAsync(RemoteSignOutContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnSignedOutCallbackRedirectAsync)));
            await s_onSignedOutCallbackRedirect(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnSignedOutCallbackRedirectAsync)));
        }
    }
}
