using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Crm;
using MyShop.Core.Models.Crm;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("CRM.View")]
    [ApiController]
    [Route("api/crm/contacts")]
    public class CrmContactsController : ControllerBase
    {
        private readonly ICrmContactService _service;

        public CrmContactsController(ICrmContactService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<CrmContactDto>>> GetAll([FromQuery] int? storeId, [FromQuery] int? companyId, [FromQuery] bool? isActive)
        {
            var list = await _service.GetAllAsync(storeId, companyId, isActive);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CrmContactDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("CRM.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreateCrmContactDto dto)
        {
            int? createdBy = null;
            if (User.FindFirst("UserId") != null && int.TryParse(User.FindFirst("UserId").Value, out var uid)) createdBy = uid;
            var id = await _service.CreateAsync(dto, createdBy);
            if (id <= 0) return BadRequest("خطا در ایجاد مخاطب.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("CRM.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCrmContactDto dto)
        {
            if (id != dto.Id) return BadRequest();
            if (!await _service.UpdateAsync(dto)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("CRM.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
