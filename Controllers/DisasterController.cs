using System.Text.Json;
using DisasterApi.Data;
using DisasterApi.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using static DisasterApi.Models.Disaster;

namespace DisasterApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class DisasterController : ControllerBase
    {
        private readonly DisasterService _service;
        private readonly InMemoryStore _store;
        private readonly IConnectionMultiplexer _redis;

        private const string CacheKey = "last_assignment";

        public DisasterController(DisasterService service, InMemoryStore store, IConnectionMultiplexer redis)
        {
            _service = service;
            _store = store;
            _redis = redis;
        }


        [HttpPost]
        [Route("areas")]
        public async Task<IActionResult> AddAreas([FromBody] List<Area> requests)
        {
            if (requests == null || !requests.Any())
                return BadRequest("No Request Data");

            foreach (var request in requests)
            {
                if (_store.Areas.Any(a => a.AreaId == request.AreaId))
                    return Conflict($"Area with ID {request.AreaId} already exists.");

                _store.Areas.Add(request);
            }

            await _redis.GetDatabase().KeyDeleteAsync(CacheKey);

            return Ok(requests);
        }

        [HttpPost]
        [Route("trucks")]
        public async Task<IActionResult> AddTrucks([FromBody] List<Truck> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest("No Request Data");
            }

            foreach (var request in requests)
            {
                if (_store.Trucks.Any(a => a.TruckId == request.TruckId))
                    return Conflict($"Truck with ID {request.TruckId} already exists.");

                _store.Trucks.Add(request);
            }

            await _redis.GetDatabase().KeyDeleteAsync(CacheKey);

            return Ok(requests);
        }

        [HttpPost]
        [Route("assignments")]
        public async Task<IActionResult> Assignments()
        {
            var db = _redis.GetDatabase();

            var cached = await db.StringGetAsync(CacheKey);

            if (!cached.IsNullOrEmpty)
            {
                var cachedResult = JsonSerializer.Deserialize<List<AssignmentResponse>>(cached.ToString());
                return Ok(new
                {
                    Source = "Redis Cache",
                    Data = cachedResult
                });
            }

            var result = _service.Assignments();

            await db.StringSetAsync(
                CacheKey,
                JsonSerializer.Serialize(result),
                TimeSpan.FromMinutes(30)
            );

            return Ok(new
            {
                Source = "Success",
                Data = result
            });
        }

        [HttpGet]
        [Route("assignments")]
        public async Task<IActionResult> GetAssignments()
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(CacheKey);

            if (cached.IsNullOrEmpty)
                return NotFound("No found assignments.");

            var result = JsonSerializer.Deserialize<List<AssignmentResponse>>(cached!);
            return Ok(result);
        }

        [HttpDelete]
        [Route("assignments")]
        public async Task<IActionResult> ClearAssignments()
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(CacheKey);

            return Ok("Successful cache cleared.");
        }
    }
}
