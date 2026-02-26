using System.Text.Json;
using DisasterApi.Services;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using static DisasterApi.Models.Disaster;

namespace DisasterApi.Tests.Services
{
    public class DisasterServiceTests
    {
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly DisasterService _service;

        public DisasterServiceTests()
        {
            _redisMock = new Mock<IConnectionMultiplexer>();
            _dbMock = new Mock<IDatabase>();

            _redisMock
                .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_dbMock.Object);

            _service = new DisasterService(_redisMock.Object);
        }

        [Fact]
        public async Task Should_Return_Message_When_No_Truck()
        {
            // Arrange
            var area = new Area
            {
                AreaId = "A1",
                UrgencyLevel = 5,
                TimeConstraint = 5,
                RequiredResources = new Dictionary<string, int>
            {
                { "food", 50 }
            }
            };

            _dbMock.Setup(x => x.HashGetAllAsync("areas", It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[]
                {
                new HashEntry("A1", JsonSerializer.Serialize(area))
                });

            _dbMock.Setup(x => x.HashGetAllAsync("trucks", It.IsAny<CommandFlags>()))
                .ReturnsAsync(Array.Empty<HashEntry>());

            // Act
            var result = await _service.AssignmentsAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].AreaId.Should().Be("A1");
            result[0].TruckId.Should().BeNull();
            result[0].Message.Should().Be("No truck meets the time constraint.");
        }

        [Fact]
        public async Task Should_Assign_Truck_When_Conditions_Match()
        {
            var area = new Area
            {
                AreaId = "A1",
                UrgencyLevel = 5,
                TimeConstraint = 10,
                RequiredResources = new Dictionary<string, int>
            {
                { "food", 50 }
            }
            };

            var truck = new Truck
            {
                TruckId = "T1",
                TravelTimeToArea = new Dictionary<string, int>
            {
                { "A1", 5 }
            },
                AvailableResources = new Dictionary<string, int>
            {
                { "food", 100 }
            }
            };

            _dbMock.Setup(x => x.HashGetAllAsync("areas", It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[]
                {
                new HashEntry("A1", JsonSerializer.Serialize(area))
                });

            _dbMock.Setup(x => x.HashGetAllAsync("trucks", It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[]
                {
                new HashEntry("T1", JsonSerializer.Serialize(truck))
                });

            var result = await _service.AssignmentsAsync();

            result.Should().HaveCount(1);
            result[0].TruckId.Should().Be("T1");
            result[0].Message.Should().BeNull();
        }
    }
}
