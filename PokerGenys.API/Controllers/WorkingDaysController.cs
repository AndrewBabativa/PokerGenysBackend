using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/working-days")]
    public class WorkingDaysController : ControllerBase
    {
        private readonly IWorkingDayService _service;

        public WorkingDaysController(IWorkingDayService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var days = await _service.GetAllAsync();
            return Ok(days);
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            // No recibe body porque crea el día actual automáticamente
            var newDay = await _service.CreateAsync();
            return CreatedAtAction(nameof(GetAll), new { id = newDay.Id }, newDay);
        }

        [HttpPatch("{id}/close")]
        public async Task<IActionResult> Close(Guid id)
        {
            await _service.CloseDayAsync(id);
            return NoContent();
        }
    }
}