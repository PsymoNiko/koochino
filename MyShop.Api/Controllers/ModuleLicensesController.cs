using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Modules;
using MyShop.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    /// <summary>Foundation API برای لایسنس ماژول‌ها (پنل فروشنده / مارکت آینده).</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [RequirePermission("Users.View")]
    public class ModuleLicensesController : ControllerBase
    {
        private readonly IModuleLicenseService _licenses;
        private readonly ICurrentTenant _tenant;

        public ModuleLicensesController(IModuleLicenseService licenses, ICurrentTenant tenant)
        {
            _licenses = licenses;
            _tenant = tenant;
        }

        [HttpGet("catalog")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCatalog([FromQuery] bool sellableOnly = true)
        {
            var items = await _licenses.GetCatalogAsync(sellableOnly);
            return Ok(items);
        }

        [HttpGet("site/{siteId:int}")]
        public async Task<IActionResult> GetLicensed(int siteId)
        {
            if (siteId <= 0) siteId = _tenant.HasSite ? _tenant.SiteId : 0;
            if (siteId <= 0) return BadRequest(new { message = "SiteId required." });
            var items = await _licenses.GetLicensedModulesAsync(siteId);
            return Ok(items);
        }

        [HttpPost("site/{siteId:int}/activate/{moduleKey}")]
        [RequirePermission("Users.Edit")]
        public async Task<IActionResult> Activate(int siteId, string moduleKey, [FromQuery] DateTime? expiresAt = null)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey))
                return BadRequest(new { message = "SiteId and moduleKey required." });
            try
            {
                await _licenses.ActivateLicenseAsync(siteId, moduleKey, expiresAt);
                return Ok(new { ok = true, siteId, moduleKey, source = "Licensed" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("site/{siteId:int}/deactivate/{moduleKey}")]
        [RequirePermission("Users.Edit")]
        public async Task<IActionResult> Deactivate(int siteId, string moduleKey)
        {
            if (siteId <= 0 || string.IsNullOrWhiteSpace(moduleKey))
                return BadRequest(new { message = "SiteId and moduleKey required." });
            try
            {
                await _licenses.DeactivateLicenseAsync(siteId, moduleKey);
                return Ok(new { ok = true, siteId, moduleKey });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
