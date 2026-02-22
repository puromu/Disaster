using DisasterApi.Data;
using static DisasterApi.Models.Disaster;

namespace DisasterApi.Services
{
    public class DisasterService
    {
        private readonly InMemoryStore _store;

        public DisasterService(InMemoryStore store)
        {
            _store = store;
        }

        public List<AssignmentResponse> Assignments()
        {
            var results = new List<AssignmentResponse>();
            var availableTrucks = new List<Truck>(_store.Trucks);

            var sortedAreas = _store.Areas
                .OrderByDescending(a => a.UrgencyLevel)
                .ToList();

            foreach (var area in sortedAreas)
            {
                bool assigned = false;
                bool anyTimeValid = false;
                bool anyResourceValid = false;

                foreach (var truck in availableTrucks)
                {
                    if (truck.TravelTimeToArea == null ||
                        !truck.TravelTimeToArea.ContainsKey(area.AreaId))
                        continue;

                    // 🟢 ตรวจว่าไปทันเวลาไหม
                    if (truck.TravelTimeToArea[area.AreaId] > area.TimeConstraint)
                        continue;

                    anyTimeValid = true;

                    // 🟢 ตรวจ resource ครบไหม
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
