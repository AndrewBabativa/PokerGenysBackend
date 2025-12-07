using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _service;

        public TablesController(ITableService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Guid dayId)
        {
            if (dayId == Guid.Empty)
                return BadRequest("El parámetro 'dayId' es obligatorio.");

            var tables = await _service.GetByDayAsync(dayId);
            return Ok(tables);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TableInstance table)
        {
            var created = await _service.CreateAsync(table);
            return CreatedAtAction(nameof(Get), new { dayId = created.DayId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TableInstance table)
        {
            table.Id = id;
            var updated = await _service.UpdateAsync(table);

            if (updated == null) return NotFound();

            return Ok(updated);
        }
    }
}