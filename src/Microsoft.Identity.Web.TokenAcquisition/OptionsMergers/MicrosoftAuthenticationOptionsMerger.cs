﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class MicrosoftIdentityApplicationOptionsMerger : IPostConfigureOptions<MicrosoftIdentityApplicationOptions>
    {
        public MicrosoftIdentityApplicationOptionsMerger(IMergedOptionsStore mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IMergedOptionsStore _mergedOptionsMonitor;

        public void PostConfigure(string name, MicrosoftIdentityApplicationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
