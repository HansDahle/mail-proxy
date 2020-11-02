using Azure.Core;
using MailProxy.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.ReverseProxy.Service.Proxy;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MailProxy.Api.Proxy
{
    public abstract class ProxyOperation
    {
        protected readonly HttpContext httpContext;
        protected readonly HttpMessageInvoker messageInvoker;

        public ProxyOperation(HttpContext httpContext, HttpMessageInvoker messageInvoker)
        {
            this.httpContext = httpContext;
            this.messageInvoker = messageInvoker;
        }

        public abstract Task HandleAsync();

        public async Task<RequestProxyOptions> CreateProxyOptionsAsync()
        {
            var credentialsProvider = httpContext.RequestServices.GetRequiredService<GraphCredentialsProvider>();

            var credentials = credentialsProvider.GetCredentials();
            var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), CancellationToken.None);

            var proxyOptions = new RequestProxyOptions()
            {
                RequestTimeout = TimeSpan.FromSeconds(100),

                // Copy all request headers except Host
                Transforms = new Transforms(
                    copyRequestHeaders: true,
                    requestTransforms: Array.Empty<RequestParametersTransform>(),
                    requestHeaderTransforms: new Dictionary<string, RequestHeaderTransform>()
                    {
                        { HeaderNames.Host, new RequestHeaderValueTransform(string.Empty, append: false) },
                        { HeaderNames.Authorization, new RequestHeaderValueTransform($"Bearer {token.Token}", append: false) }
                    },
                    responseHeaderTransforms: new Dictionary<string, ResponseHeaderTransform>(),
                    responseTrailerTransforms: new Dictionary<string, ResponseHeaderTransform>())
            };

            return proxyOptions;
        }
    }
}
