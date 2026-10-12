using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Contracts;
using MyShop.Core.Models.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [RequirePermission("Contracts.View")]
    [ApiController]
    [Route("api/contracts")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _service;

        public ContractsController(IContractService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ContractDto>>> GetAll([FromQuery] int? storeId, [FromQuery] int? companyId)
        {
            var list = await _service.GetAllAsync(storeId, companyId);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ContractDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [RequirePermission("Contracts.Create")]
        public async Task<ActionResult<int>> Create([FromBody] CreateContractDto dto)
        {
            var userIdClaim = User.FindFirst("UserId");
            int? createdBy = null;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid))
                createdBy = uid;
            var id = await _service.CreateAsync(dto, createdBy);
            if (id <= 0) return BadRequest("خطا در ایجاد قرارداد.");
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("Contracts.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContractDto dto)
        {
            if (id != dto.Id) return BadRequest();
            var ok = await _service.UpdateAsync(dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("Contracts.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
