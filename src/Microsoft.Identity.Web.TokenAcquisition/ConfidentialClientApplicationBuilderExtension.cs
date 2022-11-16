﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal static partial class ConfidentialClientApplicationBuilderExtension
    {
        public static ConfidentialClientApplicationBuilder WithClientCredentials(
            this ConfidentialClientApplicationBuilder builder,
            IEnumerable<CredentialDescription> clientCredentials,
            ILogger logger,
            ICredentialsLoader credentialsLoader,
            CredentialSourceLoaderParameters credentialSourceLoaderParameters)
        {
            foreach (var credential in clientCredentials)
            {
                if (!credential.Skip)
                {
                    // Load the credentials
                    string errorMessage = string.Empty;
                    try
                    {
                        credentialsLoader.LoadCredentialsIfNeededAsync(credential, credentialSourceLoaderParameters).GetAwaiter().GetResult();
                    }
                    catch(Exception ex)
                    {
                        errorMessage = ex.Message;
                    }

                    if (credential.CredentialType == CredentialType.SignedAssertion)
                    {
                        if (credential.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
                        {
                            if (credential.Skip)
                            {
                                Logger.NotUsingManagedIdentity(logger, errorMessage);
                            }
                            else
                            {
                                Logger.UsingManagedIdentity(logger);
                                return builder.WithClientAssertion((credential.CachedValue as ManagedIdentityClientAssertion)!.GetSignedAssertion);
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFilePath)
                        {
                            if (!credential.Skip)
                            {
                                Logger.UsingPodIdentityFile(logger, credential.SignedAssertionFileDiskPath ?? "not found");
                                return builder.WithClientAssertion((credential.CachedValue as PodIdentityClientAssertion)!.GetSignedAssertion);
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFromVault)
                        {
                            if (!credential.Skip)
                            {
                                Logger.UsingSignedAssertionFromVault(logger, credential.KeyVaultUrl ?? "undefined");
                                return builder.WithClientAssertion((credential.CachedValue as ClientAssertionProviderBase)!.GetSignedAssertion);
                            }
                        }
                    }

                    if (credential.CredentialType == CredentialType.Certificate)
                    {
                        if (credential.Certificate !=null)
                        {
                            Logger.UsingCertThumbprint(logger, credential.Certificate.Thumbprint);
                            return builder.WithCertificate(credential.Certificate);
                        }
                    }

                    if (credential.CredentialType == CredentialType.Secret)
                    {
                        return builder.WithClientSecret(credential.ClientSecret);
                    }
                }
            }

            if (clientCredentials.Any(c => c.CredentialType == CredentialType.Certificate))
            {
                throw new ArgumentException(
                    IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded,
                    nameof(clientCredentials));
            }

            return builder;
        }
    }
}
