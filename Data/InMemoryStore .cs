using static DisasterApi.Models.Disaster;

namespace DisasterApi.Data
{
    public class InMemoryStore
    {
        public List<Area> Areas { get; set; } = new();
        public List<Truck> Trucks { get; set; } = new();
    }
}
