﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    internal class JwtBearerOptionsMerger : IPostConfigureOptions<JwtBearerOptions>
    {
        public JwtBearerOptionsMerger(IMergedOptionsStore mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IMergedOptionsStore _mergedOptionsMonitor;

        public void PostConfigure(string name, JwtBearerOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromJwtBearerOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
