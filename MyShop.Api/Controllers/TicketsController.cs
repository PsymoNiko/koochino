using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Core.Models.Tickets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("Tickets.View")]
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _service;

        public TicketsController(ITicketService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<TicketDto>>> GetAll([FromQuery] int? departmentId, [FromQuery] string status)
        {
            var list = await _service.GetAllAsync(departmentId, status);
            return Ok(list);
        }

        [HttpGet("departments")]
        public async Task<ActionResult<List<TicketDepartmentItemDto>>> GetDepartments()
        {
            var list = await _service.GetDepartmentsAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("Tickets.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreateTicketDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id <= 0) return BadRequest("خطا در ایجاد تیکت.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("Tickets.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto)
        {
            if (id != dto.Id) return BadRequest();
            var ok = await _service.UpdateAsync(dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("Tickets.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
