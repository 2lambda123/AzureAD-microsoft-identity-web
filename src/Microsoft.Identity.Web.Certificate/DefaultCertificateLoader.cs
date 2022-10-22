﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Certificate Loader.
    /// Only use when loading a certificate from a daemon application, or an ASP NET app, using MSAL .NET directly.
    /// For an ASP NET Core app, <b>Microsoft Identity Web will handle the certificate loading</b> for you.
    /// <example><code>
    /// IConfidentialClientApplication app;
    /// ICertificateLoader certificateLoader = new DefaultCertificateLoader();
    ///     certificateLoader.LoadIfNeeded(config.CertificateDescription);
    ///
    ///    app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
    ///           .WithCertificate(config.CertificateDescription.Certificate)
    ///           .WithAuthority(new Uri(config.Authority))
    ///           .Build();
    /// </code></example>
    /// </summary>
    public class DefaultCertificateLoader : ICertificateLoader
    {
        /// <summary>
        /// Dictionary of credential loaders per credential source. The application can add more to 
        /// process additional credential sources(like dSMS).
        /// </summary>
        public IDictionary<CredentialSource, ICredentialLoader> CredentialLoaders { get; private set; } = new Dictionary<CredentialSource, ICredentialLoader>
        {
            { CredentialSource.KeyVault, new KeyVaultCertificateLoader() },
            { CredentialSource.Path, new FromPathCertificateLoader() },
            { CredentialSource.StoreWithThumbprint, new StoreWithThumbprintCertificateLoader() },
            { CredentialSource.StoreWithDistinguishedName, new StoreWithDistinguishedNameCertificateLoader() },
            { CredentialSource.Base64Encoded, new Base64EncodedCertificateLoader() },
        };

        /// <summary>
        ///  This default is overridable at the level of the credential description (for the certificate from KeyVault).
        /// </summary>
        public static string? UserAssignedManagedIdentityClientId
        {
            get
            {
                return KeyVaultCertificateLoader.UserAssignedManagedIdentityClientId;
            }
            set
            {
                KeyVaultCertificateLoader.UserAssignedManagedIdentityClientId = value;
            }
        }

        /// <summary>
        /// Load the credentials from the description, if needed.
        /// </summary>
        /// <param name="credentialDescription">Description of the credential.</param>
        public void LoadCredentialsIfNeeded(CredentialDescription credentialDescription)
        {
            _ = Throws.IfNull(credentialDescription);

            if (credentialDescription.CachedValue == null)
            {
                if (CredentialLoaders.TryGetValue(credentialDescription.SourceType, out ICredentialLoader? value))
                {
                    value.LoadIfNeeded(credentialDescription);
                }
            }
        }

        /// <summary>
        /// Load the first certificate from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        /// <returns>First certificate in the certificate description list.</returns>
        public static X509Certificate2? LoadFirstCertificate(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new();
            CertificateDescription? certDescription = certificateDescriptions.FirstOrDefault(c =>
            {
                defaultCertificateLoader.LoadCredentialsIfNeeded(c);
                return c.Certificate != null;
            });

            return certDescription?.Certificate;
        }

        /// <summary>
        /// Load all the certificates from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        /// <returns>All the certificates in the certificate description list.</returns>
        public static IEnumerable<X509Certificate2?> LoadAllCertificates(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new();
            if (certificateDescriptions != null)
            {
                foreach (var certDescription in certificateDescriptions)
                {
                    defaultCertificateLoader.LoadCredentialsIfNeeded(certDescription);
                    if (certDescription.Certificate != null)
                    {
                        yield return certDescription.Certificate;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all the certificates in the certificate description list.
        /// Use, for example, before a retry.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        public static void ResetCertificates(IEnumerable<CertificateDescription>? certificateDescriptions)
        {
            if (certificateDescriptions != null)
            {
                foreach (var cert in certificateDescriptions)
                {
                    cert.Certificate = null;
                }
            }
        }

        /// <summary>
        /// Load the certificate from the description, if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        public void LoadIfNeeded(CertificateDescription certificateDescription)
        {
            LoadCredentialsIfNeeded(certificateDescription);
        }
    }
}
