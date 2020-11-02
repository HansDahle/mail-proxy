using MailProxy.Api.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ReverseProxy.Service.Proxy;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MailProxy.Api.Proxy
{
    public class SubscriptionProxy : ProxyOperation
    {
        public SubscriptionProxy(HttpContext httpContext, HttpMessageInvoker messageInvoker) 
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

        private async Task<bool> AuthorizeAsync()
        {
            // Check if app is allowed to access functionality
            if (!httpContext.User.IsInRole("Proxy.Subscriptions"))
            {
                await httpContext.Response.WriteForbiddenErrorAsync("The 'Proxy.Subscriptions' role is required to manage mailbox outlook entities");
                return false;
            }


            // Must process the body to verify the resource
            try
            {
                var mailbox = await GetResourceUserAsync();

                var authorizationResult = await httpContext.AuthorizeAsync(Operations.Edit, new MailboxIdentifier(mailbox));
                if (!authorizationResult.Succeeded)
                {
                    await httpContext.Response.WriteForbiddenErrorAsync($"The app must be granted access in the proxy api, to the mailbox '{mailbox}'");
                    return false;
                }
            }
            catch (ArgumentException ex)
            {
                await httpContext.Response.WriteBadRequestAsync("InvalidInput", ex.Message);
                return false;
            }

            return true;
        }

        private async Task<string> GetResourceUserAsync()
        {
            httpContext.Request.EnableBuffering();
            var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            httpContext.Request.Body.Seek(0, SeekOrigin.Begin); // Reset stream

            var subscriptionDetails = JsonConvert.DeserializeAnonymousType(body, new { resource = string.Empty });

            var match = Regex.Match(subscriptionDetails.resource, "/users/([^/]+)/.*");
            if (!match.Success)
                throw new ArgumentException($"Only resources starting with /users/ is allowed. Found resource '{subscriptionDetails.resource}'");

            // Process the mailbox authorization
            var user = match.Groups[1].Value;

            if (string.IsNullOrEmpty(user))
                throw new ArgumentException($"User identifier not found in resource path '{subscriptionDetails.resource}'");

            return user;
        }
    }

    //public class StaticSubscriptionProxy
    //{
    //    private static HttpMessageInvoker MessageInvoker = new HttpMessageInvoker(new SocketsHttpHandler()
    //    {
    //        UseProxy = false,
    //        AllowAutoRedirect = false,
    //        AutomaticDecompression = DecompressionMethods.None,
    //        UseCookies = false
    //    });

    //    public static async Task HandleAsync(HttpContext httpContext)
    //    {
    //        #region Authorization

    //        if (!await httpContext.EnsureAuthenticatedAsync())
    //            return;

    //        if (!await AuthorizeAsync(httpContext))
    //            return;

    //        #endregion

    //        var httpProxy = httpContext.RequestServices.GetRequiredService<IHttpProxy>();


    //        var proxyOptions = await CreateProxyOptionsAsync(graphCredentials);

    //        await httpProxy.ProxyAsync(httpContext, "https://graph.microsoft.com", MessageInvoker, proxyOptions);



    //        var errorFeature = httpContext.Features.Get<IProxyErrorFeature>();
    //        if (errorFeature != null)
    //        {
    //            var error = errorFeature.Error;
    //            var exception = errorFeature.Exception;

    //            await httpContext.Response.WriteErrorAsync("ProxyError", $"Proxy operation ended with '{error}' error", exception);
    //        }
    //    }

    //    public static async Task<bool> AuthorizeAsync(HttpContext httpContext)
    //    {
    //        // Check if app is allowed to access functionality
    //        if (!httpContext.User.IsInRole("Proxy.Subscriptions"))
    //        {
    //            await httpContext.Response.WriteForbiddenErrorAsync("The 'Proxy.Subscriptions' role is required to manage mailbox outlook entities");
    //            return false;
    //        }


    //        // Must process the body to verify the resource
    //        try
    //        {
    //            var mailbox = await GetResourceUserAsync(httpContext);

    //            var authorizationResult = await httpContext.AuthorizeAsync(Operations.Edit, new MailboxIdentifier(mailbox));
    //            if (!authorizationResult.Succeeded)
    //            {
    //                await httpContext.Response.WriteForbiddenErrorAsync($"The app must be granted access in the proxy api, to the mailbox '{mailbox}'");
    //                return false;
    //            }
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            await httpContext.Response.WriteBadRequestAsync("InvalidInput", ex.Message);
    //            return false;
    //        }

    //        return true;
    //    }

    //    private static async Task<string> GetResourceUserAsync(HttpContext httpContext)
    //    {
    //        httpContext.Request.EnableBuffering();
    //        var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
    //        httpContext.Request.Body.Seek(0, SeekOrigin.Begin); // Reset stream

    //        var subscriptionDetails = JsonConvert.DeserializeAnonymousType(body, new { resource = string.Empty });

    //        var match = Regex.Match(subscriptionDetails.resource, "/users/([^/]+)/.*");
    //        if (!match.Success)
    //            throw new ArgumentException($"Only resources starting with /users/ is allowed. Found resource '{subscriptionDetails.resource}'");

    //        // Process the mailbox authorization
    //        var user = match.Groups[1].Value;

    //        if (string.IsNullOrEmpty(user))
    //            throw new ArgumentException($"User identifier not found in resource path '{subscriptionDetails.resource}'");

    //        return user;
    //    }
    //}
}
