﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    internal sealed class OutputStreamResult : ActionResult
    {
        private readonly Func<Stream, CancellationToken, Task> _action;
        private readonly string _contentType;
        private readonly string _fileDownloadName;

        public OutputStreamResult(Func<Stream, CancellationToken, Task> action, string contentType, string fileDownloadName = null)
        {
            _contentType = contentType;
            _fileDownloadName = fileDownloadName;
            _action = action;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            return context.InvokeAsync(async (token) =>
            {
                if (_fileDownloadName != null)
                {
                    ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment");
                    contentDispositionHeaderValue.FileName = _fileDownloadName;
                    context.HttpContext.Response.Headers["Content-Disposition"] = contentDispositionHeaderValue.ToString();
                }
                context.HttpContext.Response.Headers["Content-Type"] = _contentType;

#if !NETSTANDARD2_0
                context.HttpContext.Features.Get<AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();
#else
                context.HttpContext.Features.Get<AspNetCore.Http.Features.IHttpBufferingFeature>()?.DisableResponseBuffering();
#endif

                await _action(context.HttpContext.Response.Body, token);
            });
        }
    }
}