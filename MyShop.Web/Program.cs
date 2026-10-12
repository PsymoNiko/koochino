using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.SqlServer;
using MyShop.Web.Services;
using MyShop.Web.Extensions;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MudBlazor.Services;

// TLS برای APIهای خارجی (بله، ایتا) — روی برخی ویندوزسرورها TLS 1.3 باعث HandshakeFailure می‌شود
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);

// اسرار لوکال (رمز DB، توکن‌ها) — در git نیست؛ override روی appsettings.json
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// تنظیمات ماژول‌ها از فایل (override روی appsettings) — قابل تغییر از پنل ادمین
builder.Configuration.AddJsonFile("modulesettings.json", optional: true, reloadOnChange: false);

// پورت لوکال فقط خارج از IIS — روی IIS این UseUrls باعث 500.30 می‌شود
var runningInIis = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH"))
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_POOL_ID"));
if (!runningInIis && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    builder.WebHost.UseUrls("http://localhost:63871");

// اگر BackgroundService خطا بدهد، کل سایت را خاموش نکن
builder.Services.Configure<HostOptions>(o =>
{
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Razor + Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment() || string.Equals(builder.Configuration["CircuitOptions:DetailedErrors"], "true", StringComparison.OrdinalIgnoreCase);
    // موبایل/PWA: بعد از خواب کوتاه گوشی هنوز بتوان Circuit را بازگرداند
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30);
}).AddHubOptions(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    // متن بلند قوانین/ادیتور HTML از حد پیش‌فرض ۳۲کیلوبایت بیشتر است و مدار را قطع می‌کند
    // همچنین آپلود عکس (مثلاً احراز هویت، حداکثر ۵ مگابایت) از طریق InputFile روی همین مدار SignalR منتقل می‌شود.
    options.MaximumReceiveMessageSize = 5 * 1024 * 1024;
});
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 5 * 1024 * 1024;
});
builder.Services.Configure<CircuitOptions>(options =>
{
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});
builder.Services.AddHttpContextAccessor();

// MudBlazor
builder.Services.AddMudServices();

// ---------------------------
// Session Configuration
// ---------------------------
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ---------------------------
// Cookie Authentication
// ---------------------------
// ---------------------------
// Cookie Authentication (+ optional Google)
// ---------------------------
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/login-admin";
        options.LogoutPath = "/Account/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.Redirect(PortalLoginPath(ctx.Request.Path) ?? options.LoginPath);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            // اینجا کاربر لاگین است ولی دسترسی ندارد؛ تصمیم نهایی (ریدایرکت به لاگین خودش یا صفحه‌ی اخطار)
            // بر اساس نقش داخل AccessDeniedView گرفته می‌شود.
            ctx.Response.Redirect(options.AccessDeniedPath);
            return Task.CompletedTask;
        };
    })
    .AddCookie("ExternalGoogle", options =>
    {
        options.Cookie.Name = ".AspNetCore.ExternalGoogle";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = "ExternalGoogle";
        options.SaveTokens = false;
        options.Events.OnRemoteFailure = context =>
        {
            var role = GoogleLoginState.RoleFromItems(context.Properties);
            var loginPath = string.Equals(role, "store", StringComparison.OrdinalIgnoreCase)
                ? "/Account/login-store"
                : "/Account/login-customer";
            var msg = Uri.EscapeDataString("ورود Google ناموفق بود. دوباره از صفحه ورود تلاش کنید.");
            context.Response.Redirect(loginPath + "?error=" + msg);
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddPermissionAuthorization();
builder.Services.AddScoped<MyShop.Web.Services.IGoogleAuthService, MyShop.Web.Services.GoogleAuthService>();

// ---------------------------
// Database Connection — چند دیتابیس به ازای هر دامنه (Multi-Tenant)
// اگر ConnectionStrings:Tenants در appsettings پر شده باشد، به ازای هر دامنه از دیتابیس خودش استفاده می‌شود.
// ---------------------------
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString))
    throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found. Set it in appsettings.json or User Secrets.");

// Permission engine cache — same connection as app DB (after config merge)
builder.Services.AddKeyedSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(
    MyShop.Core.Models.Security.PermissionCacheKeys.ServiceKey,
    (sp, key) => new SqlServerCache(Microsoft.Extensions.Options.Options.Create(new SqlServerCacheOptions
    {
        ConnectionString = defaultConnectionString,
        SchemaName = "dbo",
        TableName = "DistributedCache"
    })));

var tenantsSection = builder.Configuration.GetSection("ConnectionStrings:Tenants");
var useMultiTenant = tenantsSection.Exists() && tenantsSection.GetChildren().Any();

if (useMultiTenant)
{
    builder.Services.AddScoped<MyShop.Core.Interfaces.ICurrentTenant, MyShop.Web.Services.CurrentTenantService>();
    builder.Services.AddScoped<IDbConnectionFactory>(sp => new MyShop.Infrastructure.Data.TenantAwareDbConnectionFactory(
        sp.GetRequiredService<MyShop.Core.Interfaces.ICurrentTenant>()));
}
else
{
    builder.Services.AddScoped<ICurrentTenant, MyShop.Web.Services.CurrentTenantService>();
    builder.Services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(defaultConnectionString));
}

// ---------------------------
// Modules (Repositories + Services) — همه ثبت می‌شوند؛ فعال/غیرفعال per-site از SiteModules + گارد مسیر
// ---------------------------
builder.Services.AddUsersModule();
builder.Services.AddStoresModule();
builder.Services.AddBlogModule();
builder.Services.AddShopModule();
builder.Services.AddCommerceModule();
builder.Services.AddPointsModule();
builder.Services.AddLuckyLeagueModule();
builder.Services.AddAnnouncementsModule();
builder.Services.AddCommonModule();
builder.Services.AddMessagingModule();
builder.Services.AddSocialMediaModule();
builder.Services.AddContractsModule();
builder.Services.AddTicketsModule();
builder.Services.AddPersonnelModule();
builder.Services.AddInventoryModule();
builder.Services.AddCrmModule();
builder.Services.AddContactModule();
builder.Services.AddReportsModule();
builder.Services.AddFinanceModule();
builder.Services.AddAssetsModule();
builder.Services.AddTrainingModule();
builder.Services.AddAcademyModule();
builder.Services.AddProductsModule();
builder.Services.AddProjectManagementModule();
builder.Services.AddCompaniesModule();
builder.Services.AddSlidersModule();
builder.Services.AddVendorPackagesModule();

builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ILicenseCallerContext, LicenseCallerContext>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IRequestAuditContext, RequestAuditContext>();
builder.Services.AddSingleton<MyShop.Application.Interfaces.Security.IEncryptionService, MyShop.Infrastructure.Security.AesGcmEncryptionService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IModuleSettingRepository, MyShop.Infrastructure.Repositories.Security.ModuleSettingRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IModuleConfigService, MyShop.Application.Services.Security.ModuleConfigService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ISiteModuleRepository, MyShop.Infrastructure.Repositories.Modules.SiteModuleRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ISiteModuleService, MyShop.Application.Services.Modules.SiteModuleService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.IModuleLicenseService, MyShop.Application.Services.Modules.ModuleLicenseService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.IModuleRegistryService, MyShop.Application.Services.Modules.ModuleRegistryService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Dashboard.IDashboardWidgetRepository, MyShop.Infrastructure.Repositories.Dashboard.DashboardWidgetRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Dashboard.IDashboardWidgetService, MyShop.Application.Services.Dashboard.DashboardWidgetService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Dashboard.IHomeSectionRepository, MyShop.Infrastructure.Repositories.Dashboard.HomeSectionRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Dashboard.IHomeSectionService, MyShop.Application.Services.Dashboard.HomeSectionService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Cms.ISitePageContentRepository, MyShop.Infrastructure.Repositories.Cms.SitePageContentRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Cms.ISitePageContentService, MyShop.Application.Services.Cms.SitePageContentService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Settings.ISiteAdminSettingsService, MyShop.Application.Services.Settings.SiteAdminSettingsService>();
builder.Services.AddScoped<MyShop.Infrastructure.Repositories.Settings.AppVersionRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Settings.IAppVersionService, AppVersionService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Settings.IPlatformContextService, PlatformContextService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Users.IUserIdentityVerificationRepository, MyShop.Infrastructure.Repositories.Users.UserIdentityVerificationRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Users.IUserIdentityVerificationService, MyShop.Application.Services.Users.UserIdentityVerificationService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Users.IUserIdentityDocumentRepository, MyShop.Infrastructure.Repositories.Users.UserIdentityDocumentRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Users.IUserIdentityDocumentService, MyShop.Application.Services.Users.UserIdentityDocumentService>();

// ---------------------------
// Web-specific services
// ---------------------------
builder.Services.AddScoped<UserDocumentUploadService>();
builder.Services.AddScoped<IdentityDocumentUploadService>();
builder.Services.AddScoped<StoreDocumentUploadService>();
builder.Services.AddScoped<TicketAttachmentUploadService>();
builder.Services.AddScoped<AcademyUploadService>();
builder.Services.AddScoped<SiteBrandIconUploadService>();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("api-auth", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});
builder.Services.AddScoped<MyShop.Web.Services.ILoginSecurityService, MyShop.Web.Services.LoginSecurityService>();
builder.Services.AddScoped<MyShop.Web.Services.IGeoLocationService, MyShop.Web.Services.GeoLocationService>();
builder.Services.AddScoped<MyShop.Web.Services.IModuleSettingsService, MyShop.Web.Services.ModuleSettingsService>();
builder.Services.AddScoped<MyShop.Web.Services.IStoreListCacheService, MyShop.Web.Services.StoreListCacheService>();
builder.Services.AddScoped<MyShop.Web.Services.IUserPointsRefreshService, MyShop.Web.Services.UserPointsRefreshService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
    options.ValueLengthLimit = 10 * 1024 * 1024;
    options.ValueCountLimit = 4096;
});

// ---------------------------
// SignalR for Real-time User Activity
// ---------------------------
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 4 * 1024 * 1024;
});

// ---------------------------
// Background Services
// ---------------------------
builder.Services.AddHostedService<UserActivityBackgroundService>();
builder.Services.AddHostedService<DailyChannelTipHostedService>();
builder.Services.AddHostedService<AcademySessionReminderHostedService>();
builder.Services.AddHostedService<StoreBillingOverdueHostedService>();
builder.Services.AddHostedService<StoreVoucherExpireHostedService>();
builder.Services.AddHostedService<SocialPublishHostedService>();
builder.Services.AddHostedService<AiEmbeddingHostedService>();
builder.Services.AddHostedService<AiKbDraftHostedService>();

// ---------------------------
// HTTP Clients for Bale and Eita
// ---------------------------
// مهم: فقط AddHttpClient<T> — AddScoped جدا HttpClient را خراب می‌کند
// روی ویندوزسرور: WinHttpHandler + IPv4 (رفع HandshakeFailure با tapi.bale.ai)
builder.Services.AddHttpClient<BaleService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(BaleHttpHandlerFactory.Create);

builder.Services.AddHttpClient<EitaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(BaleHttpHandlerFactory.Create);

builder.Services.AddHttpClient<BaleMessagingChannel>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(BaleHttpHandlerFactory.Create);
builder.Services.AddTransient<IMessagingChannel>(sp => sp.GetRequiredService<BaleMessagingChannel>());

builder.Services.AddHttpClient<TelegramMessagingChannel>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(BaleHttpHandlerFactory.Create);
builder.Services.AddTransient<IMessagingChannel>(sp => sp.GetRequiredService<TelegramMessagingChannel>());

builder.Services.AddScoped<ICommerceInvoiceService, CommerceInvoiceService>();
builder.Services.AddScoped<IAcademyCertificatePdfService, AcademyCertificatePdfService>();

builder.Services.AddHttpClient<ZarinpalPaymentGateway>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
}).AddTransientNetworkRetry();
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<ZarinpalPaymentGateway>());
builder.Services.AddScoped<IPaymentGateway, DigipayPaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, SnappPayPaymentGateway>();
builder.Services.AddScoped<IPaymentGatewayRegistry, PaymentGatewayRegistry>();

builder.Services.AddHttpClient<MyShop.Infrastructure.Messaging.SmsIrGateway>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ISmsSender>(sp => sp.GetRequiredService<MyShop.Infrastructure.Messaging.SmsIrGateway>());
builder.Services.AddScoped<ISmsAdminService>(sp => sp.GetRequiredService<MyShop.Infrastructure.Messaging.SmsIrGateway>());

// 20s keeps a slow provider from holding the Blazor circuit; the UI shows a typing indicator meanwhile.
builder.Services.AddHttpClient<OpenAiCompatibleClient>(client => client.Timeout = TimeSpan.FromSeconds(20))
    .AddTransientNetworkRetry();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiCompletionClient>(sp => sp.GetRequiredService<OpenAiCompatibleClient>());
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiEmbeddingClient>(sp => sp.GetRequiredService<OpenAiCompatibleClient>());
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiOrchestrator, MyShop.Application.Services.Ai.AiOrchestratorService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IKnowledgeRetrieval, MyShop.Application.Services.Ai.KnowledgeRetrievalService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiRuntimeOptionsProvider, MyShop.Application.Services.Ai.AiRuntimeOptionsProvider>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiUsageGuard, MyShop.Application.Services.Ai.AiUsageGuard>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IKbSeedImporter, MyShop.Application.Services.Ai.KbSeedImporter>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiConversationRepository, MyShop.Infrastructure.Repositories.Ai.AiConversationRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiUsageRepository, MyShop.Infrastructure.Repositories.Ai.AiUsageRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiInsightRepository, MyShop.Infrastructure.Repositories.Ai.AiInsightRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiKnowledgeChunkRepository, MyShop.Infrastructure.Repositories.Ai.AiKnowledgeChunkRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiEmbeddingIndexer, MyShop.Application.Services.Ai.AiEmbeddingIndexer>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiKbLearnRepository, MyShop.Infrastructure.Repositories.Ai.AiKbLearnRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiKbDraftLearner, MyShop.Application.Services.Ai.AiKbDraftLearner>();
// Personal-data skills run before retrieval; the static FAQ skill declares a lower priority and runs last.
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiSkill, MyShop.Application.Services.Ai.LoyaltySkill>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiSkill, MyShop.Application.Services.Ai.MissionSkill>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiSkill, MyShop.Application.Services.Ai.CouponSkill>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Ai.IAiSkill, MyShop.Application.Services.Ai.KnowledgeBaseSkill>();
builder.Services.AddSupportModule();
builder.Services.AddScoped<MyShop.Web.Services.SupportChatUploadService>();

builder.Services.AddHttpClient<IChannelBroadcastService, ChannelBroadcastService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}).ConfigurePrimaryHttpMessageHandler(BaleHttpHandlerFactory.Create);

// ---------------------------
// HTTP Client for FrontendAuthApi
// ---------------------------
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://api.koochino.shop/";
builder.Services.AddHttpClient<FrontendAuthApi>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddTransientNetworkRetry();

// ---------------------------
// Authentication State Provider (Session-based)
// ---------------------------
builder.Services.Configure<MyShop.Shared.JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenValidator>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// تشخیص دامنه و تنظیم tenant (connection string دیتابیس) برای این درخواست — قبل از هر چیز
app.UseMiddleware<MyShop.Web.Middleware.TenantResolutionMiddleware>();
// هدرهای امنیتی
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-XSS-Protection"] = "0";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://static.neshan.org https://accounts.google.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://static.neshan.org; " +
        "font-src 'self' data: https://fonts.gstatic.com; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' ws: wss: https://static.neshan.org https://api.neshan.org https://*.tile.openstreetmap.org https://tile.openstreetmap.org; " +
        "frame-src 'self' https://accounts.google.com; " +
        "worker-src 'self' blob:; " +
        "form-action 'self' https://accounts.google.com";
    await next();
});
app.UseResponseCompression();
app.UseWhen(ctx => !MyShop.Web.Services.ProtectedUploadEndpoints.IsProtectedPath(ctx.Request.Path),
    branch => branch.UseStaticFiles());
app.UseRouting();
app.UseRateLimiter();

// ---------------------------
// Session & Authentication Middleware
// ---------------------------
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// محافظت روت‌های ادمین: در صورت غیرفعال بودن ماژول به پیشخوان ریدایرکت
app.UseAdminModuleGuard();
// محافظت روت‌های فروشگاه‌دار: در صورت غیرفعال بودن ماژول یا نبود مجوز به پیشخوان ریدایرکت
app.UseStoreOwnerModuleGuard();
// محافظت روت‌های عمومی: در صورت غیرفعال بودن ماژول به صفحهٔ «بخش غیرفعال» ریدایرکت
app.UsePublicModuleGuard();

app.MapRazorPages();
app.MapBlazorHub();
app.MapProtectedUploads();
app.MapHub<MyShop.Web.Hubs.UserActivityHub>("/hubs/userActivity");
app.MapHub<MyShop.Web.Hubs.SupportChatHub>("/hubs/supportChat");

// خروج واقعی: پاک کردن کوکی احراز هویت در همان درخواست و ریدایرکت به صفحهٔ اصلی
app.MapGet("/Account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/", false);
}).AllowAnonymous();

app.MapGet("/Account/google-login", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    var moduleConfig = ctx.RequestServices.GetService<MyShop.Application.Interfaces.Security.IModuleConfigService>();
    var tenant = ctx.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
    var siteId = MyShop.Application.Services.Security.ModuleConfigRead.SiteIdOf(tenant);
    var clientId = await MyShop.Application.Services.Security.ModuleConfigRead.TryGetAsync(moduleConfig, siteId, "Authentication", "Google.ClientId", false)
                   ?? cfg["Authentication:Google:ClientId"];
    var clientSecret = await MyShop.Application.Services.Security.ModuleConfigRead.TryGetAsync(moduleConfig, siteId, "Authentication", "Google.ClientSecret", true)
                       ?? cfg["Authentication:Google:ClientSecret"];
    var roleRaw = (ctx.Request.Query["role"].ToString() ?? "").Trim().ToLowerInvariant();
    var role = GoogleLoginState.NormalizeRole(roleRaw);
    var isStore = role == "store";
    var loginPath = isStore ? "/Account/login-store" : "/Account/login-customer";

    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        return Results.Redirect(loginPath + "?error=" + Uri.EscapeDataString("ورود Google هنوز پیکربندی نشده است."));

    var remember = string.Equals(ctx.Request.Query["remember"], "1", StringComparison.Ordinal);
    var props = new AuthenticationProperties
    {
        RedirectUri = "/Account/google-callback"
    };
    props.Items[GoogleLoginState.RoleKey] = role;
    props.Items[GoogleLoginState.RememberKey] = remember ? "1" : "0";
    await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
    return Results.Empty;
}).AllowAnonymous();

app.MapGet("/Account/google-callback", async (HttpContext ctx) =>
{
    var result = await ctx.AuthenticateAsync("ExternalGoogle");
    var role = GoogleLoginState.RoleFromItems(result?.Properties);
    var isStore = role == "store";
    var loginPath = isStore ? "/Account/login-store" : "/Account/login-customer";
    var claimRole = isStore ? "StoreOwner" : "Customer";
    var dashboard = isStore ? "/storeowner/dashboard" : "/customer/Dashboard";

    if (result == null || !result.Succeeded || result.Principal == null)
        return Results.Redirect(loginPath + "?error=" + Uri.EscapeDataString("ورود Google ناموفق بود."));

    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value
                ?? result.Principal.FindFirst("email")?.Value;
    var subject = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? result.Principal.FindFirst("sub")?.Value;
    var given = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value
                ?? result.Principal.Identity?.Name?.Split(' ').FirstOrDefault()
                ?? "";
    var family = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ?? "";

    var googleAuth = ctx.RequestServices.GetRequiredService<IGoogleAuthService>();
    var (ok, error, user) = isStore
        ? await googleAuth.FindOrCreateStoreOwnerAsync(subject, email, given, family)
        : await googleAuth.FindOrCreateCustomerAsync(subject, email, given, family);
    await ctx.SignOutAsync("ExternalGoogle");

    if (!ok || user == null)
        return Results.Redirect(loginPath + "?error=" + Uri.EscapeDataString(error ?? "ورود Google ناموفق بود."));

    try
    {
        var authRepo = ctx.RequestServices.GetService<IAuthRepository>();
        if (authRepo != null)
            await authRepo.UpdateLastLoginAsync(user.Id);
    }
    catch { }

    var remember = GoogleLoginState.RememberFromItems(result.Properties);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username ?? email ?? ""),
        new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
        new Claim(ClaimTypes.Role, claimRole)
    };
    if (!string.IsNullOrWhiteSpace(user.FirstName))
        claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
    if (!string.IsNullOrWhiteSpace(user.LastName))
        claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
    if (!string.IsNullOrWhiteSpace(user.Email ?? email))
        claims.Add(new Claim(ClaimTypes.Email, user.Email ?? email));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(remember ? 30 : 1)
        });

    try
    {
        var activity = ctx.RequestServices.GetService<MyShop.Application.Interfaces.IUserActivityService>();
        if (activity != null)
        {
            await activity.LogLoginAsync(
                user.Id,
                ctx.Session?.Id,
                null,
                ctx.Connection?.RemoteIpAddress?.ToString(),
                ctx.Request?.Headers["User-Agent"].ToString());
        }
    }
    catch { }

    return Results.Redirect(dashboard);
}).AllowAnonymous();

// بازدید مستقیم /signin-google (بدون کد OAuth) → نه ۴۰۴؛ هدایت به لاگین.
// وقتی Google پیکربندی شده و callback واقعی است، AuthenticationMiddleware قبل از این endpoint آن را هندل می‌کند.
app.MapGet("/signin-google", () =>
{
    var msg = Uri.EscapeDataString("برای ورود با Google از دکمه موجود در صفحه ورود استفاده کنید.");
    return Results.Redirect("/Account/login-customer?error=" + msg);
}).AllowAnonymous();

// وب‌هوک بات بله: دریافت آپدیت‌ها (بدون احراز هویت)
// GET فقط برای تست دستی در مرورگر — بله همیشه POST می‌زند
app.MapGet("/api/bale-webhook", () => Results.Json(new
{
    ok = true,
    message = "Bale webhook endpoint is live. Prefer /api/bots/Bale/{siteCode}/webhook for multi-site.",
    path = "/api/bale-webhook",
    multiSite = "/api/bots/{provider}/{siteCode}/webhook"
})).AllowAnonymous();

app.MapPost("/api/bale-webhook", async (HttpContext ctx) =>
{
    var resolver = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Messaging.IBotCredentialsResolver>();
    var tenant = ctx.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
    var creds = await resolver.ResolveAsync(MyShop.Core.Models.Messaging.MessagingProviders.Bale, tenant?.SiteId);

    var expectedSecret = creds?.WebhookSecret
        ?? ctx.RequestServices.GetRequiredService<IConfiguration>()["Bale:WebhookSecret"];
    var denied = await MyShop.Web.Services.WebhookSecretGuard.CheckAsync(ctx, expectedSecret, tenant?.SiteId, "Messaging", "Bale.WebhookSecret");
    if (denied != null) return denied;

    string body;
    using (var reader = new StreamReader(ctx.Request.Body))
        body = await reader.ReadToEndAsync();
    var baleService = ctx.RequestServices.GetRequiredService<MyShop.Web.Services.BaleService>();
    await baleService.ProcessUpdateAsync(body, creds);
    return Results.Ok();
}).AllowAnonymous();

// وب‌هوک چندباتی: /api/bots/Bale/koochino/webhook  یا  /api/bots/Telegram/koochino/webhook
app.MapPost("/api/bots/{provider}/{siteCode}/webhook", async (HttpContext ctx, string provider, string siteCode) =>
{
    var siteService = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Sites.ISiteService>();
    var site = await siteService.GetByCodeAsync(siteCode);
    if (site == null || !site.IsActive)
        return Results.NotFound(new { ok = false, message = "سایت یافت نشد." });

    var resolver = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Messaging.IBotCredentialsResolver>();
    var creds = await resolver.ResolveAsync(provider, site.Id);
    if (creds == null || !creds.IsActive)
        return Results.NotFound(new { ok = false, message = "بات برای این سایت فعال نیست." });

    var denied = await MyShop.Web.Services.WebhookSecretGuard.CheckAsync(ctx, creds.WebhookSecret, site.Id, "Messaging", provider + ".WebhookSecret");
    if (denied != null) return denied;

    string body;
    using (var reader = new StreamReader(ctx.Request.Body))
        body = await reader.ReadToEndAsync();

    // فعلاً پردازش آپدیت با همان منطق بله (API سازگار با تلگرام)
    var baleService = ctx.RequestServices.GetRequiredService<MyShop.Web.Services.BaleService>();
    await baleService.ProcessUpdateAsync(body, creds);
    return Results.Ok(new { ok = true, site = site.Code, provider = creds.Provider });
}).AllowAnonymous();

app.MapGet("/api/bots/{provider}/{siteCode}/webhook", (string provider, string siteCode) => Results.Json(new
{
    ok = true,
    message = "Set this URL as bot webhook (POST).",
    provider,
    siteCode,
    path = $"/api/bots/{provider}/{siteCode}/webhook"
})).AllowAnonymous();

// وب‌هوک اختصاصی پشتیبانی (بات جدا از OTP) — همان دامنه Plesk
app.MapPost("/api/support/{provider}/{siteCode}/webhook", async (HttpContext ctx, string provider, string siteCode) =>
{
    var siteService = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Sites.ISiteService>();
    var site = await siteService.GetByCodeAsync(siteCode);
    if (site == null || !site.IsActive)
        return Results.NotFound(new { ok = false, message = "سایت یافت نشد." });

    var resolver = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Messaging.IBotCredentialsResolver>();
    var creds = await resolver.ResolveAsync(provider, site.Id);
    if (creds == null || !creds.IsActive)
        return Results.NotFound(new { ok = false, message = "بات برای این سایت فعال نیست." });

    var denied = await MyShop.Web.Services.WebhookSecretGuard.CheckAsync(ctx, creds.WebhookSecret, site.Id, "Messaging", provider + ".WebhookSecret");
    if (denied != null) return denied;

    string body;
    using (var reader = new StreamReader(ctx.Request.Body))
        body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Ok(new { ok = true });

    var omni = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.SupportChat.ISupportOmnichannelService>();
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (!root.TryGetProperty("message", out var messageEl))
            return Results.Ok(new { ok = true });
        var chatType = messageEl.TryGetProperty("chat", out var chatTypeEl)
            && chatTypeEl.TryGetProperty("type", out var typeEl)
            ? typeEl.GetString()
            : null;
        if (!string.Equals(chatType, "private", StringComparison.OrdinalIgnoreCase))
            return Results.Ok(new { ok = true });
        var chatId = messageEl.TryGetProperty("chat", out var chatEl) && chatEl.TryGetProperty("id", out var idEl)
            ? idEl.GetRawText().Trim('"')
            : null;
        var text = messageEl.TryGetProperty("text", out var t) ? t.GetString()?.Trim() : null;
        string name = null;
        if (messageEl.TryGetProperty("from", out var fromEl) && fromEl.TryGetProperty("first_name", out var fn))
            name = fn.GetString();
        if (!string.IsNullOrWhiteSpace(chatId) && !string.IsNullOrWhiteSpace(text))
        {
            var inbound = string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "/support", StringComparison.OrdinalIgnoreCase)
                ? "سلام، نیاز به پشتیبانی دارم."
                : text;
            var previous = MyShop.Core.Models.Messaging.BotExecutionContext.Current;
            try
            {
                MyShop.Core.Models.Messaging.BotExecutionContext.Current = creds;
                await omni.HandleInboundAsync(creds.Provider, site.Id, chatId, name, inbound);
            }
            finally { MyShop.Core.Models.Messaging.BotExecutionContext.Current = previous; }
        }
    }
    catch { /* بدنه نامعتبر */ }
    return Results.Ok(new { ok = true, site = site.Code, provider = creds.Provider });
}).AllowAnonymous();

app.MapGet("/api/support/{provider}/{siteCode}/webhook", (string provider, string siteCode) => Results.Json(new
{
    ok = true,
    message = "Support omnichannel webhook (POST). Use for a dedicated support bot, or enable intercept on the shared bot.",
    provider,
    siteCode,
    path = $"/api/support/{provider}/{siteCode}/webhook"
})).AllowAnonymous();

// Social Media parallel webhook — does NOT replace /api/bots/...
// For Telegram/Bale: after intake, forwards to the same BaleService so bot/OTP keep working if URL is switched.
app.MapMethods("/api/social/{provider}/{siteCode}/webhook", new[] { "GET", "POST" }, async (HttpContext ctx, string provider, string siteCode) =>
{
    var siteService = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Sites.ISiteService>();
    var site = await siteService.GetByCodeAsync(siteCode);
    if (site == null || !site.IsActive)
        return Results.NotFound(new { ok = false, message = "سایت یافت نشد." });

    var isInstagram = string.Equals(provider, MyShop.Core.Models.SocialMedia.SocialProviderKeys.Instagram, StringComparison.OrdinalIgnoreCase);

    // Instagram Meta challenge (GET)
    if (HttpMethods.IsGet(ctx.Request.Method) && isInstagram)
    {
        var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var expected = (cfg["Social:Instagram:VerifyToken"] ?? cfg["Instagram:VerifyToken"] ?? "").Trim();
        var provided = (ctx.Request.Query["hub.verify_token"].ToString() ?? "").Trim();
        if (string.IsNullOrEmpty(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            return Results.Unauthorized();

        var intake = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.SocialMedia.ISocialWebhookIntakeService>();
        var challengeResult = await intake.IngestAsync(new MyShop.Application.Interfaces.SocialMedia.SocialWebhookContext
        {
            Provider = provider,
            SiteCode = siteCode,
            SiteId = site.Id,
            QueryHubMode = ctx.Request.Query["hub.mode"],
            QueryHubChallenge = ctx.Request.Query["hub.challenge"],
            QueryHubVerifyToken = ctx.Request.Query["hub.verify_token"]
        });
        if (!string.IsNullOrEmpty(challengeResult.ChallengeResponse))
            return Results.Text(challengeResult.ChallengeResponse, "text/plain");
        return Results.Json(new { ok = true, provider, siteCode, path = $"/api/social/{provider}/{siteCode}/webhook" });
    }

    if (HttpMethods.IsGet(ctx.Request.Method))
    {
        return Results.Json(new
        {
            ok = true,
            message = "Social webhook live. Prefer /api/bots/{provider}/{siteCode}/webhook for production bots.",
            provider,
            siteCode,
            path = $"/api/social/{provider}/{siteCode}/webhook",
            legacyBotWebhook = $"/api/bots/{provider}/{siteCode}/webhook"
        });
    }

    // Secret check for bot providers via SiteBots (same as legacy)
    MyShop.Core.Models.Messaging.BotCredentials creds = null;
    if (!isInstagram)
    {
        var resolver = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.Messaging.IBotCredentialsResolver>();
        creds = await resolver.ResolveAsync(provider, site.Id);
        if (creds == null || !creds.IsActive)
            return Results.NotFound(new { ok = false, message = "بات برای این سایت فعال نیست." });

        var denied = await MyShop.Web.Services.WebhookSecretGuard.CheckAsync(ctx, creds.WebhookSecret, site.Id, "Messaging", provider + ".WebhookSecret");
        if (denied != null) return denied;
    }

    string body;
    using (var reader = new StreamReader(ctx.Request.Body))
        body = await reader.ReadToEndAsync();

    var socialIntake = ctx.RequestServices.GetRequiredService<MyShop.Application.Interfaces.SocialMedia.ISocialWebhookIntakeService>();
    var ingest = await socialIntake.IngestAsync(new MyShop.Application.Interfaces.SocialMedia.SocialWebhookContext
    {
        Provider = provider,
        SiteCode = siteCode,
        SiteId = site.Id,
        Body = body
    });

    if (ingest.ForwardToLegacyBotProcessor && creds != null)
    {
        var baleService = ctx.RequestServices.GetRequiredService<MyShop.Web.Services.BaleService>();
        await baleService.ProcessUpdateAsync(body, creds);
    }

    return Results.Ok(new { ok = true, site = site.Code, provider, social = true, forwarded = ingest.ForwardToLegacyBotProcessor });
}).AllowAnonymous();

// PWA manifest پویا بر اساس برند سایت جاری
app.MapGet("/api/site-manifest", async (HttpContext ctx) =>
{
    var tenant = ctx.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
    var siteService = ctx.RequestServices.GetService<MyShop.Application.Interfaces.Sites.ISiteService>();
    MyShop.Core.Models.Sites.SiteBrandCopy brand = null;
    if (tenant?.HasSite == true && siteService != null)
    {
        try { brand = await siteService.GetBrandAsync(tenant.SiteId); } catch { /* fallback */ }
    }

    var name = !string.IsNullOrWhiteSpace(brand?.SiteName)
        ? brand.SiteName
        : (!string.IsNullOrWhiteSpace(tenant?.SiteName) ? tenant.SiteName : "کوچینو");
    var icon192 = !string.IsNullOrWhiteSpace(brand?.PwaIcon192Url)
        ? brand.PwaIcon192Url
        : "/img/weblogo-192.png?v=2";
    var icon512 = !string.IsNullOrWhiteSpace(brand?.PwaIcon512Url)
        ? brand.PwaIcon512Url
        : "/img/weblogo-512.png?v=2";

    var manifest = new
    {
        name,
        short_name = name.Length > 12 ? name.Substring(0, 12) : name,
        description = "سیستم مدیریت فروشگاه و کاربران",
        id = "/",
        start_url = "/?source=pwa",
        scope = "/",
        display = "standalone",
        display_override = new[] { "standalone", "minimal-ui" },
        background_color = "#ffffff",
        theme_color = !string.IsNullOrWhiteSpace(brand?.PrimaryColor) ? brand.PrimaryColor : "#ffffff",
        orientation = "portrait-primary",
        icons = new object[]
        {
            new { src = icon192, sizes = "192x192", type = "image/png", purpose = "any" },
            new { src = icon512, sizes = "512x512", type = "image/png", purpose = "any" },
            new { src = icon192, sizes = "192x192", type = "image/png", purpose = "maskable" },
            new { src = icon512, sizes = "512x512", type = "image/png", purpose = "maskable" }
        },
        categories = new[] { "business", "productivity", "shopping" },
        lang = "fa",
        dir = "rtl",
        prefer_related_applications = false
    };
    return Results.Json(manifest, contentType: "application/manifest+json");
}).AllowAnonymous();

// ثبت نصب PWA — تعداد نصب، IP، User-Agent، مشخصات دستگاه (AllowAnonymous)
app.MapPost("/api/pwa/installed", async (HttpContext ctx) =>
{
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "";
    if (ip == "::1") ip = "127.0.0.1";
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    int? userId = null;
    // فقط از claim احراز هویت — نه از بدنه کلاینت
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var claim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User.FindFirst("UserId")?.Value;
        if (int.TryParse(claim, out var uid) && uid > 0)
            userId = uid;
    }
    string platform = null, language = null, timeZone = null, deviceType = null, extraInfo = null;
    int? screenWidth = null, screenHeight = null, hardwareConcurrency = null;
    decimal? deviceMemory = null;
    try
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var json = await reader.ReadToEndAsync();
        if (!string.IsNullOrWhiteSpace(json))
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("platform", out var p)) platform = p.GetString();
            if (root.TryGetProperty("language", out var l)) language = l.GetString();
            if (root.TryGetProperty("timezone", out var t)) timeZone = t.GetString();
            if (root.TryGetProperty("deviceType", out var d)) deviceType = d.GetString();
            if (root.TryGetProperty("screenWidth", out var sw)) screenWidth = sw.GetInt32();
            if (root.TryGetProperty("screenHeight", out var sh)) screenHeight = sh.GetInt32();
            if (root.TryGetProperty("hardwareConcurrency", out var hc)) hardwareConcurrency = hc.GetInt32();
            if (root.TryGetProperty("deviceMemory", out var dm)) deviceMemory = (decimal?)dm.GetDouble();
            if (root.TryGetProperty("extraInfo", out var ex)) extraInfo = ex.GetString();
        }
    }
    catch { /* نادیده گرفتن خطای بدنه */ }
    var service = ctx.RequestServices.GetService<MyShop.Application.Interfaces.IPwaInstallLogService>();
    if (service == null) return Results.Json(new { ok = false, message = "Service not registered" }, statusCode: 400);
    await service.LogInstallAsync(userId, ip, userAgent, deviceType, platform, language, screenWidth, screenHeight, hardwareConcurrency, deviceMemory, timeZone, extraInfo, null);
    return Results.Json(new { ok = true });
}).AllowAnonymous();

app.MapGet("/sitemap.xml", async (HttpContext ctx) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var sb = new System.Text.StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
    sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

    void AddUrl(string path, string changefreq, string priority)
    {
        sb.Append("<url><loc>").Append(System.Net.WebUtility.HtmlEncode(baseUrl + path)).Append("</loc>")
          .Append("<changefreq>").Append(changefreq).Append("</changefreq>")
          .Append("<priority>").Append(priority).Append("</priority></url>\n");
    }

    AddUrl("/", "daily", "1.0");
    AddUrl("/shop", "daily", "0.9");
    AddUrl("/stores", "daily", "0.8");

    try
    {
        var tenant = ctx.RequestServices.GetService<MyShop.Core.Interfaces.ICurrentTenant>();
        var siteId = tenant?.HasSite == true ? (int?)tenant.SiteId : null;
        var shopCatalog = ctx.RequestServices.GetService<MyShop.Application.Interfaces.Commerce.ICommerceShopCatalogService>();
        var products = shopCatalog != null ? await shopCatalog.ListVisibleAsync(siteId) : null;
        if (products != null)
            foreach (var p in products) AddUrl($"/shop/product/{p.Id}", "weekly", "0.7");
    }
    catch { /* یک بخش ناموفق نباید کل نقشهٔ سایت را خراب کند */ }

    try
    {
        var storeService = ctx.RequestServices.GetService<MyShop.Application.Interfaces.IStoreService>();
        var stores = storeService != null ? await storeService.GetListForPublicAsync(null, false, "Latest") : null;
        if (stores != null)
            foreach (var s in stores) AddUrl($"/store/{s.StoreID}", "weekly", "0.6");
    }
    catch { }

    try
    {
        var postService = ctx.RequestServices.GetService<MyShop.Application.Interfaces.IPostService>();
        var posts = postService != null ? await postService.GetAllAsync(true) : null;
        if (posts != null)
            foreach (var p in posts) AddUrl($"/post/{p.Id}", "monthly", "0.5");
    }
    catch { }

    sb.Append("</urlset>");
    return Results.Text(sb.ToString(), "application/xml; charset=utf-8");
}).AllowAnonymous();

app.MapFallbackToPage("/_Host");

static string PortalLoginPath(PathString path)
{
    var p = path.Value ?? "";
    if (p.StartsWith("/academy", StringComparison.OrdinalIgnoreCase))
        return "/Account/login-academy-student";
    if (p.StartsWith("/instructor", StringComparison.OrdinalIgnoreCase))
        return "/Account/login-academy-instructor";
    if (p.StartsWith("/customer", StringComparison.OrdinalIgnoreCase))
        return "/Account/login-customer";
    if (p.StartsWith("/storeowner", StringComparison.OrdinalIgnoreCase) || p.StartsWith("/store-owner", StringComparison.OrdinalIgnoreCase))
        return "/Account/login-store";
    return null;
}

app.Run();
