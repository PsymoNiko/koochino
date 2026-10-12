using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.IdentityModel.Tokens;
using MyShop.Api.Auth;
using MyShop.Api.Services;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Assets;
using MyShop.Application.Interfaces.Contracts;
using MyShop.Application.Interfaces.Crm;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Companies;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Application.Interfaces.Personnel;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Application.Interfaces.Reports;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Application.Interfaces.Training;
using MyShop.Application.Services.Assets;
using MyShop.Application.Services.Contracts;
using MyShop.Application.Services.Crm;
using MyShop.Application.Services.Finance;
using MyShop.Application.Services.Companies;
using MyShop.Application.Services.Inventory;
using MyShop.Application.Services.Personnel;
using MyShop.Application.Services.Products;
using MyShop.Application.Services.ProjectManagement;
using MyShop.Application.Services.Reports;
using MyShop.Application.Services.Tickets;
using MyShop.Application.Services.Training;
using MyShop.Core.Interfaces;
using MyShop.Infrastructure.Data;
using MyShop.Infrastructure.Repository;
using MyShop.Shared;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Infrastructure.Repositories.Security;
using MyShop.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddHttpContextAccessor();



// ----------------------------
// JWT Settings
// ----------------------------
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var keyBytes = Encoding.UTF8.GetBytes(jwt.Key);

// ----------------------------
// Controllers + Swagger
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();





// ----------------------------
// JWT Authentication
// ----------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
// Keyed IDistributedCache dedicated to the permission engine (SQL Server-backed, locked
// decision) — same keyed pattern as MyShop.Web, see sql/security/V010. Only ever touched
// once Authorization:UsePermissionEngine is on, which requires V001-V010 to be applied.
builder.Services.AddKeyedSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(
    MyShop.Core.Models.Security.PermissionCacheKeys.ServiceKey,
    (sp, key) => new SqlServerCache(Microsoft.Extensions.Options.Options.Create(new SqlServerCacheOptions
    {
        ConnectionString = builder.Configuration.GetConnectionString("Default"),
        SchemaName = "dbo",
        TableName = "DistributedCache"
    })));
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IConditionEvaluator, DenyConditionalByDefaultEvaluator>();
builder.Services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
builder.Services.AddScoped<IPortalPermissionService, PortalPermissionService>();
builder.Services.AddScoped<IDataScopeResolver, DataScopeResolver>();
builder.Services.AddScoped<ISecurityContext, ApiSecurityContext>();

// ----------------------------
// Database Connection — چند دیتابیس به ازای هر دامنه (Multi-Tenant)
// ----------------------------
var defaultConn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(defaultConn))
    throw new Exception("❌ ConnectionString 'Default' not found in appsettings.json");

var tenantsSection = builder.Configuration.GetSection("ConnectionStrings:Tenants");
var useMultiTenant = tenantsSection.Exists() && tenantsSection.GetChildren().Any();

if (useMultiTenant)
{
    builder.Services.AddScoped<ICurrentTenant, MyShop.Api.Services.CurrentTenantService>();
    builder.Services.AddScoped<IDbConnectionFactory>(sp => new TenantAwareDbConnectionFactory(
        sp.GetRequiredService<ICurrentTenant>()));
}
else
{
    builder.Services.AddScoped<ICurrentTenant, MyShop.Api.Services.CurrentTenantService>();
    builder.Services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(defaultConn));
}

// ----------------------------
// Repository + Auth service
// ----------------------------
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, ApiAuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MyShop.Application.Interfaces.Sites.ISiteRepository, MyShop.Infrastructure.Repositories.Sites.SiteRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Sites.ISiteService, MyShop.Application.Services.Sites.SiteService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ISiteModuleRepository, MyShop.Infrastructure.Repositories.Modules.SiteModuleRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ISiteModuleService, MyShop.Application.Services.Modules.SiteModuleService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.ILicenseCallerContext, MyShop.Api.Services.LicenseCallerContext>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IRequestAuditContext, MyShop.Api.Services.RequestAuditContext>();
builder.Services.AddSingleton<MyShop.Application.Interfaces.Security.IEncryptionService, MyShop.Infrastructure.Security.AesGcmEncryptionService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IModuleSettingRepository, MyShop.Infrastructure.Repositories.Security.ModuleSettingRepository>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Security.IModuleConfigService, MyShop.Application.Services.Security.ModuleConfigService>();
builder.Services.AddScoped<MyShop.Application.Interfaces.Modules.IModuleLicenseService, MyShop.Application.Services.Modules.ModuleLicenseService>();

// Contracts module (API)
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractService, ContractService>();
// Tickets module (API)
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
// Personnel module (API)
builder.Services.AddScoped<IPersonnelRepository, PersonnelRepository>();
builder.Services.AddScoped<IPersonnelService, PersonnelService>();
// Inventory module (API)
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
// CRM module (API)
builder.Services.AddScoped<ICrmContactRepository, CrmContactRepository>();
builder.Services.AddScoped<ICrmContactService, CrmContactService>();
// Reports module (API)
builder.Services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
builder.Services.AddScoped<IReportDefinitionService, ReportDefinitionService>();
// Finance module (API)
builder.Services.AddScoped<IFinanceAccountRepository, FinanceAccountRepository>();
builder.Services.AddScoped<IFinanceAccountService, FinanceAccountService>();
// Assets module (API)
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAssetService, AssetService>();
// Training module (API)
builder.Services.AddScoped<ITrainingCourseRepository, TrainingCourseRepository>();
builder.Services.AddScoped<ITrainingCourseService, TrainingCourseService>();
// Products module (API)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
// ProjectManagement module (API)
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
// Companies module (API)
builder.Services.AddScoped<IHoldingRepository, HoldingRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IHoldingService, HoldingService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api-auth", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Success = false,
            Message = "تعداد درخواست‌ها بیش از حد مجاز است. کمی بعد تلاش کنید."
        }, token);
    };
});

var app = builder.Build();

// ----------------------------
// Exception Handling (امنیت: جزئیات خطا فقط در Development)
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// ----------------------------
// Swagger
// ----------------------------

// Swagger فقط در محیط Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "koch API V1");
        c.RoutePrefix = string.Empty;
    });
}

// ----------------------------
// online host policy
// ----------------------------

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("WebPolicy", policy =>
//    {
//        policy
//            .WithOrigins("https://navankha.ir/")
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});



app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

app.UseMiddleware<MyShop.Api.Middleware.TenantResolutionMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<MyShop.Api.Middleware.AuthRateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// endpoint خطا برای Production (بدون افشای جزئیات)
app.MapGet("/error", () => Results.Json(new { error = "An error occurred.", status = 500 }, statusCode: 500))
    .ExcludeFromDescription();

app.MapControllers();
app.Run();
