﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal static class Constants
    {
        // IssuerMetadata
        public const string TenantDiscoveryEndpoint = "tenant_discovery_endpoint";
        public const string ApiVersion = "api-version";
        public const string Metadata = "metadata";

        // Metadata
        public const string PreferredNetwork = "preferred_network";
        public const string PreferredCache = "preferred_cache";
        public const string Aliases = "aliases";

        // AadIssuerValidator
        public const string AzureADIssuerMetadataUrl = "https://login.microsoftonline.com/common/discovery/instance?authorization_endpoint=https://login.microsoftonline.com/common/oauth2/v2.0/authorize&api-version=1.1";
        public const string FallbackAuthority = "https://login.microsoftonline.com/";

        // RegisterValidAudience
        public const string Version = "ver";
        public const string V1 = "1.0";
        public const string V2 = "2.0";

        // ClaimsPrincipalExtension
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string Consumers = "consumers";
        public const string Organizations = "organizations";

        // ClientInfo
        public const string Uid = "uid";
        public const string Utid = "utid";
        public const string ClientInfo = "client_info";
        public const string One = "1";

        // Miscellaneous
        public const string UserAgent = "User-Agent";
        public const string JwtSecurityTokenUsedToCallWebAPI = "JwtSecurityTokenUsedToCallWebAPI";
        public const string AzureAd = "AzureAd";
        public const string PreferredUserName = "preferred_username";
        public const string NameClaim = "name";
    }
}
