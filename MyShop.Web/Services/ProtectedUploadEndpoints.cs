using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Core.Models.AdminAccess;

namespace MyShop.Web.Services
{
    public static class ProtectedUploadEndpoints
    {
        public static bool IsProtectedPath(PathString path)
        {
            var p = path.Value ?? "";
            return p.StartsWith("/uploads/users/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("/uploads/tickets/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("/uploads/support/", StringComparison.OrdinalIgnoreCase);
        }

        public static IEndpointRouteBuilder MapProtectedUploads(this IEndpointRouteBuilder app)
        {
            app.MapGet("/uploads/users/{userId:int}/{fileName}", ServeUserFile).RequireAuthorization();
            app.MapGet("/uploads/tickets/{ticketId:int}/{fileName}", ServeTicketFile).RequireAuthorization();
            app.MapGet("/uploads/support/{chatKey}/{fileName}", ServeSupportFile);
            return app;
        }

        static IResult ServeFile(IWebHostEnvironment env, string relativeDir, string fileName)
        {
            var safe = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safe) || safe != fileName)
                return Results.NotFound();
            var full = Path.GetFullPath(Path.Combine(env.WebRootPath, relativeDir, safe));
            var root = Path.GetFullPath(Path.Combine(env.WebRootPath, relativeDir));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
                return Results.NotFound();
            return Results.File(full, ContentType(safe));
        }

        static string ContentType(string name)
        {
            var ext = Path.GetExtension(name)?.ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        static async Task<IResult> ServeUserFile(HttpContext ctx, int userId, string fileName)
        {
            if (!await CanViewUserDocsAsync(ctx, userId))
                return Results.Forbid();
            var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return ServeFile(env, Path.Combine("uploads", "users", userId.ToString()), fileName);
        }

        static async Task<IResult> ServeTicketFile(HttpContext ctx, int ticketId, string fileName)
        {
            var tickets = ctx.RequestServices.GetRequiredService<ITicketService>();
            var ticket = await tickets.GetByIdAsync(ticketId);
            if (ticket == null) return Results.NotFound();
            var uid = UserId(ctx.User);
            var canAdmin = await HasTicketAdminAsync(ctx);
            if (!canAdmin && (!ticket.UserId.HasValue || ticket.UserId.Value != uid))
                return Results.Forbid();
            var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return ServeFile(env, Path.Combine("uploads", "tickets", ticketId.ToString()), fileName);
        }

        static async Task<IResult> ServeSupportFile(HttpContext ctx, string chatKey, string fileName)
        {
            if (!Guid.TryParseExact(chatKey, "N", out var chatId) && !Guid.TryParse(chatKey, out chatId))
                return Results.NotFound();
            var chats = ctx.RequestServices.GetRequiredService<ISupportChatService>();
            var chat = await chats.GetAsync(chatId);
            if (chat == null) return Results.NotFound();

            var uid = UserId(ctx.User);
            var canAdmin = await HasSupportAdminAsync(ctx);
            var owner = chat.UserId.HasValue && chat.UserId.Value == uid;
            var guestOk = ctx.RequestServices.GetService<SupportChatAccessTokenService>()
                ?.TryAuthorize(chatId, chat.SiteId) == true;
            if (!canAdmin && !owner && !guestOk)
                return Results.Unauthorized();

            var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return ServeFile(env, Path.Combine("uploads", "support", chatId.ToString("N")), fileName);
        }

        static async Task<bool> CanViewUserDocsAsync(HttpContext ctx, int userId)
        {
            var user = ctx.User;
            if (user?.Identity?.IsAuthenticated != true) return false;
            if (UserId(user) == userId) return true;
            var perms = ctx.RequestServices.GetService<IAdminPermissionService>();
            return await PermissionCheckHelper.HasAdminModuleAsync(user, perms, "Users", AdminPermissionActions.Read);
        }

        static Task<bool> HasTicketAdminAsync(HttpContext ctx)
        {
            var perms = ctx.RequestServices.GetService<IAdminPermissionService>();
            return PermissionCheckHelper.HasAdminModuleAsync(ctx.User, perms, "Tickets", AdminPermissionActions.Read);
        }

        static Task<bool> HasSupportAdminAsync(HttpContext ctx)
        {
            var perms = ctx.RequestServices.GetService<IAdminPermissionService>();
            return PermissionCheckHelper.HasAdminModuleAsync(ctx.User, perms, "Support", AdminPermissionActions.Read);
        }

        static int UserId(ClaimsPrincipal user)
        {
            var v = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user?.FindFirst("UserId")?.Value;
            return int.TryParse(v, out var id) ? id : 0;
        }
    }
}
