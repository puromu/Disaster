using System.Text.Json;
using DisasterApi.Controllers;
using DisasterApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackExchange.Redis;
using static DisasterApi.Models.Disaster;

namespace DisasterApi.Tests.Controllers
{
    public class DisasterControllerTests
    {
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDisasterService> _serviceMock;
        private readonly DisasterController _controller;

        public DisasterControllerTests()
        {
            _dbMock = new Mock<IDatabase>();
            _redisMock = new Mock<IConnectionMultiplexer>();
            _serviceMock = new Mock<IDisasterService>();

            _redisMock
                .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_dbMock.Object);

            _controller = new DisasterController(
                _serviceMock.Object,
                _redisMock.Object
            );
        }

        [Fact]
        public async Task AddAreas_Should_Return_Conflict_When_Exists()
        {
            // Arrange
            var request = new Area { AreaId = "A1" };

            _dbMock.Setup(x => x.HashExistsAsync("areas", "A1", It.IsAny<CommandFlags>()))
                   .ReturnsAsync(true);

            // Act
            var result = await _controller.AddAreas(new List<Area> { request });

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Assignments_Should_Return_From_Cache_When_Exists()
        {
            var cachedData = JsonSerializer.Serialize(new List<AssignmentResponse>
        {
            new AssignmentResponse { AreaId = "A1" }
        });

            _dbMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                   .ReturnsAsync(cachedData);

            var result = await _controller.Assignments();

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
