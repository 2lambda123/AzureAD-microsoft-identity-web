﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension class containing cookie policies (work around for same site).
    /// </summary>
    public static class CookiePolicyOptionsExtensions
    {
        /// <summary>
        /// Handles SameSite cookie issue according to the https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1.
        /// The default list of user-agents that disallow SameSite None, was taken from https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/.
        /// </summary>
        /// <param name="options"><see cref="CookiePolicyOptions"/>to update.</param>
        /// <returns><see cref="CookiePolicyOptions"/> to chain.</returns>
        public static CookiePolicyOptions HandleSameSiteCookieCompatibility(this CookiePolicyOptions options)
        {
            return HandleSameSiteCookieCompatibility(options, DisallowsSameSiteNone);
        }

        /// <summary>
        /// Handles SameSite cookie issue according to the docs: https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
        /// The default list of user-agents that disallow SameSite None, was taken from https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/.
        /// </summary>
        /// <param name="options"><see cref="CookiePolicyOptions"/>to update.</param>
        /// <param name="disallowsSameSiteNone">If you don't want to use the default user-agent list implementation, the method sent in this parameter will be run against the user-agent and if returned true, SameSite value will be set to Unspecified. The default user-agent list used can be found at: https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/. </param>
        /// <returns><see cref="CookiePolicyOptions"/> to chain.</returns>
        public static CookiePolicyOptions HandleSameSiteCookieCompatibility(this CookiePolicyOptions options, Func<string, bool> disallowsSameSiteNone)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            options.OnAppendCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions, disallowsSameSiteNone);
            options.OnDeleteCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions, disallowsSameSiteNone);

            return options;
        }

        private static void CheckSameSite(HttpContext httpContext, CookieOptions options, Func<string, bool> disallowsSameSiteNone)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (disallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        /// <summary>
        /// Checks if the specified user agent supports SameSite None cookies.
        /// </summary>
        /// <param name="userAgent">Browser user agent.</param>
        /// <remarks>Method taken from https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/.</remarks>
        /// <returns>True, if the user agent does not allow SameSite None cookie; otherwise, false.</returns>
        public static bool DisallowsSameSiteNone(string userAgent)
        {
            if (!string.IsNullOrEmpty(userAgent))
            {
                // Cover all iOS based browsers here. This includes:
                // - Safari on iOS 12 for iPhone, iPod Touch, iPad
                // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
                // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
                // All of which are broken by SameSite=None, because they use the iOS networking
                // stack.
                if (userAgent.Contains("CPU iPhone OS 12") ||
                    userAgent.Contains("iPad; CPU OS 12"))
                {
                    return true;
                }

                // Cover Mac OS X based browsers that use the Mac OS networking stack.
                // This includes:
                // - Safari on Mac OS X.
                // This does not include:
                // - Chrome on Mac OS X
                // Because they do not use the Mac OS networking stack.
                if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                    userAgent.Contains("Version/") && userAgent.Contains("Safari"))
                {
                    return true;
                }

                // Cover Chrome 50-69, because some versions are broken by SameSite=None,
                // and none in this range require it.
                // Note: this covers some pre-Chromium Edge versions,
                // but pre-Chromium Edge does not require SameSite=None.
                if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
