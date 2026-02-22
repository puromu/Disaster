using System.Text.Json;
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
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private const string AreaKey = "areas";
        private const string TruckKey = "trucks";
        private const string CacheKey = "last_assignment";

        public DisasterController(DisasterService service, IConnectionMultiplexer redis)
        {
            _service = service;
            _redis = redis;
            _db = _redis.GetDatabase();
        }


        [HttpPost]
        [Route("areas")]
        public async Task<IActionResult> AddAreas([FromBody] List<Area> requests)
        {
            if (requests == null || !requests.Any())
                return BadRequest("No Request Data");

            foreach (var request in requests)
            {
                var exists = await _db.HashExistsAsync(AreaKey, request.AreaId);

                if (exists)
                    return Conflict($"Area with ID {request.AreaId} already exists.");

                await _db.HashSetAsync(
                    AreaKey,
                    request.AreaId,
                    JsonSerializer.Serialize(request)
                );
            }

            await _db.KeyDeleteAsync(CacheKey);

            return Ok(requests);
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var entries = await _db.HashGetAllAsync(AreaKey);

            var areas = entries
                .Select(e => JsonSerializer.Deserialize<Area>(e.Value!))
                .ToList();

            if (areas == null)
                return NotFound("NO Data");

            return Ok(areas);
        }

        [HttpPost]
        [Route("trucks")]
        public async Task<IActionResult> AddTrucks([FromBody] List<Truck> requests)
        {
            if (requests == null || !requests.Any())
                return BadRequest("No Request Data");

            foreach (var request in requests)
            {
                var exists = await _db.HashExistsAsync(TruckKey, request.TruckId);

                if (exists)
                    return Conflict($"Truck with ID {request.TruckId} already exists.");

                await _db.HashSetAsync(
                    TruckKey,
                    request.TruckId,
                    JsonSerializer.Serialize(request)
                );
            }

            await _db.KeyDeleteAsync(CacheKey);

            return Ok(requests);
        }

        [HttpGet("trucks")]
        public async Task<IActionResult> GetTrucks()
        {
            var entries = await _db.HashGetAllAsync(TruckKey);

            var trucks = entries
                .Select(e => JsonSerializer.Deserialize<Truck>(e.Value!))
                .ToList();

            if (trucks == null)
                return NotFound("NO Data");

            return Ok(trucks);
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

            var result = await _service.AssignmentsAsync();

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

            await _db.KeyDeleteAsync(AreaKey);
            await _db.KeyDeleteAsync(TruckKey);
            await _db.KeyDeleteAsync(CacheKey);

            return Ok("Successful cache cleared.");
        }
    }
}
