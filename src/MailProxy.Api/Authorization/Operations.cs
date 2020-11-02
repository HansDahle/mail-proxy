using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Collections.Generic;

namespace MailProxy.Api.Authorization
{
    public static class Operations
    {
        public static OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement() { Name = "EDIT" };
    }
}
