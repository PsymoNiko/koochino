using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Personnel;
using MyShop.Core.Models.Personnel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("Personnel.View")]
    [ApiController]
    [Route("api/personnel")]
    public class PersonnelController : ControllerBase
    {
        private readonly IPersonnelService _service;

        public PersonnelController(IPersonnelService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PersonnelDto>>> GetAll([FromQuery] int? storeId, [FromQuery] int? companyId, [FromQuery] bool? isActive)
        {
            var list = await _service.GetAllAsync(storeId, companyId, isActive);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PersonnelDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("Personnel.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreatePersonnelDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id <= 0) return BadRequest("خطا در ایجاد پرسنل.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("Personnel.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePersonnelDto dto)
        {
            if (id != dto.Id) return BadRequest();
            var ok = await _service.UpdateAsync(dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("Personnel.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
