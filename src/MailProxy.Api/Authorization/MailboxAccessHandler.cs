using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MailProxy.Api.Authorization
{
    public class MailboxAccessHandler : AuthorizationHandler<OperationAuthorizationRequirement, MailboxIdentifier>
    {
        private readonly ILogger<MailboxAccessHandler> logger;
        private readonly IApplicationMailboxResolver appResolver;

        public MailboxAccessHandler(ILogger<MailboxAccessHandler> logger, IApplicationMailboxResolver appResolver)
        {
            this.logger = logger;
            this.appResolver = appResolver;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, MailboxIdentifier resource)
        {
            var appIdValue = context.User.FindFirstValue("appid");
            if (!Guid.TryParse(appIdValue, out Guid appId))
            {
                logger.LogWarning("Could not locate any appid claim to determin the application that is requesting access");
                return;
            }

            var ownedMailboxes = await appResolver.ResolveOwnedMailboxesAsync(appId);
            logger.LogInformation($"Located owned mailboxes: {string.Join(",", ownedMailboxes)}");


            if (ownedMailboxes.Any(m => string.Equals(resource.Mail, m, StringComparison.OrdinalIgnoreCase)))
                context.Succeed(requirement);


            logger.LogInformation($"The requested mailbox is not owned by the application '{appId}'");
        }
    }
}
