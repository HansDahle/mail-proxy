using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailProxy.Api.Authentication
{

    public class GraphCredentialsProvider
    {
        private readonly ClientSecretCredential credential;

        public GraphCredentialsProvider(IConfiguration configuration)
        {
            this.credential = new ClientSecretCredential(configuration["AzureAd:TenantId"], configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"]);
        }

        public TokenCredential GetCredentials()
        {
            return credential;
        }
    }
}
