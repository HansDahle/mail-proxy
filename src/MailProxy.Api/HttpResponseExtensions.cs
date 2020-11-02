using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MailProxy.Api
{
    public static class HttpResponseExtensions
    {
        public static bool IsSuccessfulResponse(this HttpResponse response)
        {
            return response.StatusCode >= 200 && response.StatusCode < 300;
        }

        public static async Task WriteErrorAsync(this HttpResponse response, string code, string message, Exception ex)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 500;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonConvert.SerializeObject(new
            {

                error = new
                {
                    code = code,
                    message = message,
                    innerError = new
                    {
                        type = ex.GetType().Name,
                        message = ex.Message
                    }
                }
            }, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
        }

        public static async Task WriteBadRequestAsync(this HttpResponse response, string code, string message)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 400;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonConvert.SerializeObject(new
            {
                error = new
                {
                    code = code,
                    message = message
                }
            }, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
        }

        public static async Task WriteForbiddenErrorAsync(this HttpResponse response, string message)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 403;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonConvert.SerializeObject(new
            {
                error = new
                {
                    code = "Forbidden",
                    message = message
                }
            }, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
        }
    }

    public static class HttpContextExtensions
    {
        public static async Task<TResponse> DispatchCommandAsync<TRequest, TResponse>(this HttpContext httpContext, TRequest command)
            where TRequest : IRequest<TResponse>
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            var response = await mediator.Send(command);
            return response;
        }

        public static async Task<TResponse> DispatchQueryAsync<TResponse>(this HttpContext httpContext, IRequest<TResponse> query)
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            var response = await mediator.Send(query);
            return response;
        }

        public static async Task DispatchCommandAsync<TRequest>(this HttpContext httpContext, TRequest command)
            where TRequest : IRequest
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            await mediator.Send(command);
        }

        public static async Task<AuthorizationResult> AuthorizeAsync(this HttpContext httpContext, IAuthorizationRequirement requirement, object resource)
        {
            var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            var result = await authorizationService.AuthorizeAsync(httpContext.User, resource, requirement);
            return result;
        }

        public static async Task<bool> EnsureAuthenticatedAsync(this HttpContext httpContext)
        {
            var authResult = await httpContext.AuthenticateAsync();

            if (!authResult.Succeeded)
            {
                httpContext.Response.StatusCode = 401;
                return false;
            }

            return true;
        }
    }

}
