using System.Text.Json;
using StackExchange.Redis;
using static DisasterApi.Models.Disaster;

namespace DisasterApi.Services
{
    public class DisasterService : IDisasterService
    {
        private readonly IDatabase _db;
        private const string AreaKey = "areas";
        private const string TruckKey = "trucks";
        public DisasterService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<List<AssignmentResponse>> AssignmentsAsync()
        {
            var results = new List<AssignmentResponse>();

            // 🔹 ดึง Areas จาก Redis
            var areaEntries = await _db.HashGetAllAsync(AreaKey);
            var areas = areaEntries
                .Select(e => JsonSerializer.Deserialize<Area>(e.Value!))
                .Where(a => a != null)
                .OrderByDescending(a => a!.UrgencyLevel)
                .ToList();

            // 🔹 ดึง Trucks จาก Redis
            var truckEntries = await _db.HashGetAllAsync(TruckKey);
            var availableTrucks = truckEntries
                .Select(e => JsonSerializer.Deserialize<Truck>(e.Value!))
                .Where(t => t != null)
                .ToList();

            foreach (var area in areas!)
            {
                bool assigned = false;
                bool anyTimeValid = false;
                bool anyResourceValid = false;

                foreach (var truck in availableTrucks!)
                {
                    if (truck!.TravelTimeToArea == null ||
                        !truck.TravelTimeToArea.ContainsKey(area!.AreaId))
                        continue;

                    if (truck.TravelTimeToArea[area.AreaId] > area.TimeConstraint)
                        continue;

                    anyTimeValid = true;

                    bool canFulfill = true;

                    if (area.RequiredResources == null ||
                        truck.AvailableResources == null)
                    {
                        canFulfill = false;
                    }
                    else
                    {
                        foreach (var required in area.RequiredResources)
                        {
                            if (!truck.AvailableResources.ContainsKey(required.Key) ||
                                truck.AvailableResources[required.Key] < required.Value)
                            {
                                canFulfill = false;
                                break;
                            }
                        }
                    }

                    if (!canFulfill)
                        continue;

                    anyResourceValid = true;

                    results.Add(new AssignmentResponse
                    {
                        AreaId = area.AreaId,
                        TruckId = truck.TruckId,
                        ResourcesDelivered = area.RequiredResources,
                        Message = null
                    });

                    availableTrucks.Remove(truck);
                    assigned = true;
                    break;
                }

                if (!assigned)
                {
                    string reason;

                    if (!anyTimeValid)
                        reason = "No truck meets the time constraint.";
                    else if (!anyResourceValid)
                        reason = "Insufficient resources.";
                    else
                        reason = "No suitable truck available.";

                    results.Add(new AssignmentResponse
                    {
                        AreaId = area.AreaId,
                        TruckId = null,
                        ResourcesDelivered = null,
                        Message = reason
                    });
                }
            }

            return results;
        }
    }
}
