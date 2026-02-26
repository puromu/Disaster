namespace DisasterApi.Models
{
    public class Disaster
    {
        public class Area
        {
            public string AreaId { get; set; } = string.Empty;
            public int? UrgencyLevel { get; set; }
            public Dictionary<string, int> RequiredResources { get; set; } = new Dictionary<string, int>();
            public int? TimeConstraint { get; set; }
        }
        public class Truck
        {
            public string TruckId { get; set; } = string.Empty;

            public Dictionary<string, int> AvailableResources { get; set; } = new Dictionary<string, int>();

            public Dictionary<string, int> TravelTimeToArea { get; set; } = new Dictionary<string, int>();
        }
        public class AssignmentResponse
        {
            public string? AreaId { get; set; }
            public string? TruckId { get; set; }
            public Dictionary<string, int>? ResourcesDelivered { get; set; }
            public string? Message { get; set; }
        }
    }
}