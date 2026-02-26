using static DisasterApi.Models.Disaster;

namespace DisasterApi.Services
{
    public interface IDisasterService
    {
        Task<List<AssignmentResponse>> AssignmentsAsync();
    }
}
