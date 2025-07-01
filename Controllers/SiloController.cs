using Microsoft.AspNetCore.Mvc;
using MonitorApi.Data;
using MonitorApi.Models;
using MonitorApi.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorApi.Controllers
{
    [ApiController]
    public class SiloController : ControllerBase
    {
        private readonly MonitorDbContext _db;
        private readonly DataForwardingService _forwarder;

        public SiloController(MonitorDbContext db, DataForwardingService forwarder)
        {
            _db = db;
            _forwarder = forwarder;
        }

        [HttpPost("api/silo")]
        public async Task<IActionResult> Post([FromBody] TransitionDto dto)
        {
            var record = new LogRecord { State = dto.State, Timestamp = dto.Timestamp };
            _db.LogRecords.Add(record);
            await _db.SaveChangesAsync();

            // Forward to downstream API
            await _forwarder.ForwardAsync(dto);

            return Accepted();
        }

        [HttpGet("api/silo")]
        public IActionResult Get()
        {
            var list = _db.LogRecords.OrderByDescending(r => r.Id).ToList();
            return Ok(list);
        }
    }
}