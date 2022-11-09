﻿using Microsoft.AspNetCore.Builder;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension class on IApplicationBuilder to initialize the service provider of
    /// the TokenAcquirerFactory in ASP.NET Core.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses the token acquirer factory, therefore 
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseTokenAcquirerFactory(this IApplicationBuilder applicationBuilder)
        {
            TokenAcquirerFactory.GetDefaultInstance().ServiceProvider = applicationBuilder.ApplicationServices;
            return applicationBuilder;
        }
    }
}
