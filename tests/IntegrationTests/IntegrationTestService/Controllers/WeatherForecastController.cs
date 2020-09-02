﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

namespace IntegrationTestService.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("SecurePage")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "user_impersonation" };

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        [HttpGet]
        public async Task<string> GetAsync()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { "User.Read" }).ConfigureAwait(false);
        }
    }
}
