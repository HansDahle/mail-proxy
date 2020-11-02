using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MailProxy.Api
{
    public interface IApplicationMailboxResolver
    {
        Task<IEnumerable<string>> ResolveOwnedMailboxesAsync(Guid appId);
    }
}
