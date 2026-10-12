using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class UserPointsRefreshService : IUserPointsRefreshService
    {
        private readonly List<Func<Task>> _handlers = new List<Func<Task>>();

        public void RegisterForRefresh(Func<Task> handler)
        {
            if (handler != null && !_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public async Task NotifyPointsChangedAsync()
        {
            foreach (var handler in _handlers.ToArray())
            {
                try
                {
                    await handler();
                }
                catch
                {
                    // ignore per-handler errors
                }
            }
        }
    }
}
