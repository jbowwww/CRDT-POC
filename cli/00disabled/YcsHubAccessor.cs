using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Ycs.Hubs;

namespace Ycs.Middleware
{
    public class YcsHubAccessor
    {
        private static readonly Lazy<YcsHubAccessor> _instance = new(() => new YcsHubAccessor());

        private YcsHubAccessor()
        {
            // Do nothing.
        }

        public static YcsHubAccessor Instance => _instance.Value;

        public IHubContext<YcsHub>? YcsHub { get; internal set; }
    }

    /// <summary>
    /// This remains left-over from the 'YcsSample' app in the Ycs repo.
    /// I am leaving it here as a note of how one could get access to the YcsHub[Accessor] instances
    /// if this were implemented within, say, an ASP.NET MVC project (i.e. with controllers and HTTP methods, etc)
    /// </summary>
    public static class YcsHubAccessorMiddlewareExtensions
    {
        public static IApplicationBuilder UseYcsHubAccessor(this IApplicationBuilder appBuilder)
        {
            return appBuilder.Use(async (context, next) =>
            {
                YcsHubAccessor.Instance.YcsHub = context.RequestServices.GetRequiredService<IHubContext<YcsHub>>();

                if (next != null)
                {
                    await next.Invoke();
                }
            });
        }
    }
}
