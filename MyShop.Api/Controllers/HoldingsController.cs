using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Companies;
using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("Companies.View")]
    [ApiController]
    [Route("api/holdings")]
    public class HoldingsController : ControllerBase
    {
        private readonly IHoldingService _service;

        public HoldingsController(IHoldingService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<HoldingDto>>> GetAll([FromQuery] bool? isActive)
        {
            var list = await _service.GetAllAsync(isActive);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<HoldingDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("Companies.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreateHoldingDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id <= 0) return BadRequest("خطا در ایجاد هلدینگ.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("Companies.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateHoldingDto dto)
        {
            if (id != dto.Id) return BadRequest();
            if (!await _service.UpdateAsync(dto)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("Companies.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
