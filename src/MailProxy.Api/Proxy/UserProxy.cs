using MailProxy.Api.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ReverseProxy.Service.Proxy;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MailProxy.Api.Proxy
{
    public class UserProxy : ProxyOperation
    {
        public UserProxy(HttpContext httpContext, HttpMessageInvoker messageInvoker)
            : base(httpContext, messageInvoker)
        {
        }

        public override async Task HandleAsync()
        {
            #region Authorization

            if (!await httpContext.EnsureAuthenticatedAsync())
                return;

            if (!await AuthorizeAsync())
                return;

            #endregion

            var httpProxy = httpContext.RequestServices.GetRequiredService<IHttpProxy>();
            var proxyOptions = await CreateProxyOptionsAsync();

            await httpProxy.ProxyAsync(httpContext, "https://graph.microsoft.com", messageInvoker, proxyOptions);

            var errorFeature = httpContext.Features.Get<IProxyErrorFeature>();
            if (errorFeature != null)
            {
                var error = errorFeature.Error;
                var exception = errorFeature.Exception;

                await httpContext.Response.WriteErrorAsync("ProxyError", $"Proxy operation ended with '{error}' error", exception);
            }
        }

        public async Task<bool> AuthorizeAsync()
        {
            // Check if app is allowed to access functionality
            if (!httpContext.User.IsInRole("Proxy.Exchange.ReadWrite"))
            {
                await httpContext.Response.WriteForbiddenErrorAsync("The 'Proxy.Exchange.ReadWrite' role is required to manage mailbox outlook entities");
                return false;
            }

            // Process the mailbox authorization
            var mailbox = httpContext.Request.RouteValues["mailbox"] as string;

            if (mailbox is null)
            {
                await httpContext.Response.WriteBadRequestAsync("InvalidInput", "Missing user identifier");
                return false;
            }
            else
            {
                var authorizationResult = await httpContext.AuthorizeAsync(Operations.Edit, new MailboxIdentifier(mailbox));
                if (!authorizationResult.Succeeded)
                {
                    await httpContext.Response.WriteForbiddenErrorAsync($"The app must be granted access in the proxy api, to the user mailbox '{mailbox}'");
                    return false;
                }
            }

            return true;
        }
    }
}
