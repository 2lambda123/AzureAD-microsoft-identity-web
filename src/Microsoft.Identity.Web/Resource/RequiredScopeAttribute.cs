﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// For use in web APIs to validate scopes on controller actions.
    /// </summary>
    public class RequiredScopeAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <remarks>When the scopes don't match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        public RequiredScopeAttribute(params string[] acceptedScopes)
            : base(typeof(RequiredScopeFilter))
        {
            Arguments = new object[] { acceptedScopes };
        }
    }
}
