using Microsoft.AspNetCore.Mvc;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Services;
using System;
using System.Threading.Tasks;

namespace PokerGenys.API.Controllers
{
    [ApiController]
    [Route("api/tables")]
    public class CashTablesController : ControllerBase
    {
        private readonly ICashTableService _service; // Usamos la interfaz correcta

        public CashTablesController(ICashTableService service)
        {
            _service = service;
        }

        [HttpGet]
        // CORRECCIÓN: Renombramos 'dayId' a 'workingDayId' para coincidir con el Frontend y el Dominio
        public async Task<IActionResult> Get([FromQuery] Guid workingDayId)
        {
            if (workingDayId == Guid.Empty)
                return BadRequest("El parámetro 'workingDayId' es obligatorio.");

            var tables = await _service.GetByDayAsync(workingDayId);
            return Ok(tables);
        }

        // Obtener una mesa por ID específico (Útil para refrescar solo una mesa)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            // Nota: Si tu servicio no tiene GetById, puedes implementarlo o filtrar
            // Si no lo tienes, este endpoint es opcional pero recomendado
            /* var table = await _service.GetByIdAsync(id); 
            if (table == null) return NotFound();
            return Ok(table); 
            */
            return NotFound("Endpoint GetById no implementado en Service aún");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CashTable table)
        {
            try
            {
                var created = await _service.CreateAsync(table);
                // Ajustamos el CreatedAtAction para usar el nuevo nombre de parámetro
                return CreatedAtAction(nameof(Get), new { workingDayId = created.WorkingDayId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CashTable table)
        {
            if (id != table.Id) return BadRequest("ID mismatch");

            try
            {
                var updated = await _service.UpdateAsync(table);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}