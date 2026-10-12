using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Core.Models.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("Inventory.View")]
    [ApiController]
    [Route("api/warehouses")]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehousesController(IWarehouseService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<WarehouseDto>>> GetAll([FromQuery] int? storeId, [FromQuery] int? companyId, [FromQuery] bool? isActive)
        {
            var list = await _service.GetAllAsync(storeId, companyId, isActive);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WarehouseDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("Inventory.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreateWarehouseDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id <= 0) return BadRequest("خطا در ایجاد انبار.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("Inventory.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseDto dto)
        {
            if (id != dto.Id) return BadRequest();
            if (!await _service.UpdateAsync(dto)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("Inventory.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
