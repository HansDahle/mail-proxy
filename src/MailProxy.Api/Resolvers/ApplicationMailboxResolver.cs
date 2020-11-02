using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MailProxy.Api.Resolvers
{

    public class ApplicationMailboxResolver : IApplicationMailboxResolver
    {
        private readonly IMemoryCache cache;

        public ApplicationMailboxResolver(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public Task<IEnumerable<string>> ResolveOwnedMailboxesAsync(Guid appId)
        {
            var config = GetApplicationConfigs();

            if (config.TryGetValue(appId, out List<string>? allowedMailboxes))
                return Task.FromResult(allowedMailboxes.AsEnumerable());

            return Task.FromResult(Array.Empty<string>().AsEnumerable());
        }

        private Dictionary<Guid, List<string>> GetApplicationConfigs()
        {
            return cache.GetOrCreate("mailboxAccess", i =>
            {
                i.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var configData = File.ReadAllText("mailboxAccess.json");
                var parsedData = JsonConvert.DeserializeAnonymousType(configData, new
                {
                    applications = new[]
                    {
                    new
                    {
                        appId = Guid.Empty,
                        mailboxes = Array.Empty<string>()
                    }
                }
                });

                var config = parsedData.applications.ToDictionary(a => a.appId, a => a.mailboxes.ToList());
                return config;
            });
        }
    }
}
