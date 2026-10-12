using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;

namespace MyShop.Web.Hubs
{
    [Authorize]
    public class UserActivityHub : Hub
    {
        private readonly IUserActivityService _activityService;

        public UserActivityHub(IUserActivityService activityService)
        {
            _activityService = activityService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var sessionId = Context.GetHttpContext()?.Session?.Id;
                var ipAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString();
                var userAgent = Context.GetHttpContext()?.Request?.Headers["User-Agent"].ToString();

                // ثبت لاگین
                await _activityService.LogLoginAsync(
                    userId.Value,
                    sessionId,
                    Context.ConnectionId,
                    ipAddress,
                    userAgent
                );

                // اضافه کردن به گروه Admin (برای اطلاع‌رسانی)
                var userRole = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole == "Admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                    // اطلاع‌رسانی به Admin ها
                    await Clients.Group("Admins").SendAsync("UserOnline", userId.Value);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var sessionId = Context.GetHttpContext()?.Session?.Id;

                // ثبت Logout
                await _activityService.LogLogoutAsync(
                    userId.Value,
                    sessionId,
                    Context.ConnectionId
                );

                // اطلاع‌رسانی به Admin ها
                await Clients.Group("Admins").SendAsync("UserOffline", userId.Value);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Heartbeat برای نگه داشتن session فعال
        public async Task SendHeartbeat()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var sessionId = Context.GetHttpContext()?.Session?.Id;
                await _activityService.UpdateHeartbeatAsync(
                    userId.Value,
                    sessionId,
                    Context.ConnectionId
                );
            }
        }

        private int? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
