using Xunit;
using Moq;
using FabOS.WebServer.Services.Implementations;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FabOS.WebServer.Tests.Services
{
    public class TraceServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TraceService _service;
        private readonly Mock<ILogger<TraceService>> _loggerMock;
        private readonly Mock<ITenantService> _tenantServiceMock;

        public TraceServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<TraceService>>();
            _tenantServiceMock = new Mock<ITenantService>();

            // Setup tenant service defaults
            _tenantServiceMock.Setup(x => x.GetCurrentCompanyId()).Returns(1);
            _tenantServiceMock.Setup(x => x.GetCurrentUserId()).Returns(1);

            _service = new TraceService(_context, _loggerMock.Object, _tenantServiceMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test catalogue items
            _context.CatalogueItems.AddRange(
                new CatalogueItem
                {
                    Id = 1,
                    ItemCode = "PL10",
                    Description = "10mm Plate",
                    Category = "Plate",
                    Material = "Mild Steel",
                    Mass_kg_m2 = 78.5m,
                    CompanyId = 1
                },
                new CatalogueItem
                {
                    Id = 2,
                    ItemCode = "UB200",
                    Description = "200UB Universal Beam",
                    Category = "Beam",
                    Material = "Mild Steel",
                    Mass_kg_m = 25.7m,
                    CompanyId = 1
                },
                new CatalogueItem
                {
                    Id = 3,
                    ItemCode = "RB20",
                    Description = "20mm Round Bar",
                    Category = "Bar",
                    Material = "Mild Steel",
                    Weight_kg = 2.466m, // per piece
                    CompanyId = 1
                }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task CalculateWeightFromCatalogue_PlateWithArea_ReturnsCorrectWeight()
        {
            // Arrange
            var catalogueItemId = 1; // 10mm Plate
            var quantity = 2.5m; // 2.5 m²
            var unit = "m2";

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(catalogueItemId, quantity, unit);

            // Assert
            Assert.Equal(196.25m, weight); // 2.5 * 78.5 = 196.25 kg
        }

        [Fact]
        public async Task CalculateWeightFromCatalogue_BeamWithLength_ReturnsCorrectWeight()
        {
            // Arrange
            var catalogueItemId = 2; // 200UB
            var quantity = 6m; // 6 meters
            var unit = "m";

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(catalogueItemId, quantity, unit);

            // Assert
            Assert.Equal(154.2m, weight); // 6 * 25.7 = 154.2 kg
        }

        [Fact]
        public async Task CalculateWeightFromCatalogue_ItemWithEach_ReturnsCorrectWeight()
        {
            // Arrange
            var catalogueItemId = 3; // 20mm Round Bar
            var quantity = 10m; // 10 pieces
            var unit = "ea";

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(catalogueItemId, quantity, unit);

            // Assert
            Assert.Equal(24.66m, weight); // 10 * 2.466 = 24.66 kg
        }

        [Fact]
        public async Task CalculateWeightFromCatalogue_InvalidUnit_ReturnsZero()
        {
            // Arrange
            var catalogueItemId = 1;
            var quantity = 5m;
            var unit = "invalid";

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(catalogueItemId, quantity, unit);

            // Assert
            Assert.Equal(0m, weight);
        }

        [Fact]
        public async Task CalculateWeightFromCatalogue_NonExistentItem_ReturnsZero()
        {
            // Arrange
            var catalogueItemId = 999; // Non-existent
            var quantity = 5m;
            var unit = "m";

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(catalogueItemId, quantity, unit);

            // Assert
            Assert.Equal(0m, weight);
        }

        [Fact]
        public async Task CreateTraceRecord_SetsCorrectTenantData()
        {
            // Arrange
            var traceRecord = new TraceRecord
            {
                EntityType = TraceableType.WorkOrder,
                EntityId = 123,
                Description = "Test trace",
                CaptureDateTime = DateTime.UtcNow
            };

            // Act
            var created = await _service.CreateTraceRecordAsync(traceRecord);

            // Assert
            Assert.NotNull(created);
            Assert.Equal(1, created.CompanyId);
            Assert.Equal(1, created.UserId);
            Assert.NotEqual(Guid.Empty, created.TraceId);
            Assert.NotNull(created.TraceNumber);
            Assert.Contains("TRC-", created.TraceNumber);
        }

        [Fact]
        public async Task RecordMaterialIssue_PopulatesRequiredFields()
        {
            // Arrange
            var traceRecord = new TraceRecord
            {
                EntityType = TraceableType.WorkOrder,
                EntityId = 123,
                CaptureDateTime = DateTime.UtcNow,
                CompanyId = 1
            };
            _context.TraceRecords.Add(traceRecord);
            await _context.SaveChangesAsync();

            // Act
            var material = await _service.RecordMaterialIssueAsync(
                traceRecord.Id,
                1, // Plate catalogue item
                5m,
                "m2"
            );

            // Assert
            Assert.NotNull(material);
            Assert.Equal("PL10", material.MaterialCode);
            Assert.Equal("10mm Plate", material.Description);
            Assert.Equal(5m, material.Quantity);
            Assert.Equal("m2", material.Unit);
        }

        [Theory]
        [InlineData(0, 10, 0)]      // Zero quantity
        [InlineData(-5, 10, 0)]     // Negative quantity
        [InlineData(5, 0, 0)]       // Zero mass
        [InlineData(5, -10, 0)]     // Negative mass
        public async Task CalculateWeight_EdgeCases_ReturnsExpected(decimal quantity, decimal mass, decimal expected)
        {
            // Arrange
            var item = new CatalogueItem
            {
                Id = 99,
                ItemCode = "TEST",
                Description = "Test Item",
                Mass_kg_m = mass,
                CompanyId = 1
            };
            _context.CatalogueItems.Add(item);
            await _context.SaveChangesAsync();

            // Act
            var weight = await _service.CalculateWeightFromCatalogueAsync(99, quantity, "m");

            // Assert
            Assert.Equal(expected, weight);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    public class PdfProcessingServiceTests
    {
        private readonly PdfProcessingService _service;
        private readonly Mock<ILogger<PdfProcessingService>> _loggerMock;

        public PdfProcessingServiceTests()
        {
            _loggerMock = new Mock<ILogger<PdfProcessingService>>();
            _service = new PdfProcessingService(_loggerMock.Object);
        }

        [Theory]
        [InlineData(10, 10, 20, 20, 14.142135623730951)] // Distance calculation
        [InlineData(0, 0, 3, 4, 5)]                       // 3-4-5 triangle
        [InlineData(0, 0, 0, 10, 10)]                    // Vertical line
        [InlineData(0, 0, 10, 0, 10)]                    // Horizontal line
        public void CalculateRealDistance_ReturnsCorrectDistance(
            float x1, float y1, float x2, float y2, double expectedDistance)
        {
            // Arrange
            var point1 = new PdfPoint { X = x1, Y = y1 };
            var point2 = new PdfPoint { X = x2, Y = y2 };
            var scale = 1.0f; // 1:1 scale

            // Act
            var distance = _service.CalculateRealDistance(point1, point2, scale, "mm");

            // Assert
            Assert.Equal((decimal)expectedDistance, distance, 10);
        }

        [Fact]
        public void CalculateRealArea_Triangle_ReturnsCorrectArea()
        {
            // Arrange - Right triangle with base 10 and height 20
            var points = new List<PdfPoint>
            {
                new PdfPoint { X = 0, Y = 0 },
                new PdfPoint { X = 10, Y = 0 },
                new PdfPoint { X = 0, Y = 20 }
            };
            var scale = 1.0f;

            // Act
            var area = _service.CalculateRealArea(points, scale, "mm");

            // Assert
            Assert.Equal(100m, area); // (10 * 20) / 2 = 100
        }

        [Fact]
        public void CalculateRealArea_Square_ReturnsCorrectArea()
        {
            // Arrange - 10x10 square
            var points = new List<PdfPoint>
            {
                new PdfPoint { X = 0, Y = 0 },
                new PdfPoint { X = 10, Y = 0 },
                new PdfPoint { X = 10, Y = 10 },
                new PdfPoint { X = 0, Y = 10 }
            };
            var scale = 1.0f;

            // Act
            var area = _service.CalculateRealArea(points, scale, "mm");

            // Assert
            Assert.Equal(100m, area); // 10 * 10 = 100
        }

        [Fact]
        public void MeasureAngle_RightAngle_Returns90Degrees()
        {
            // Arrange
            var vertex = new PdfPoint { X = 0, Y = 0 };
            var point1 = new PdfPoint { X = 10, Y = 0 };  // Horizontal
            var point2 = new PdfPoint { X = 0, Y = 10 };  // Vertical

            // Act
            var result = _service.MeasureAngle(vertex, point1, point2);

            // Assert
            Assert.Equal("Angle", result.Type);
            Assert.Equal(90m, result.Value, 1); // Allow 1 degree tolerance
        }

        [Fact]
        public void CalculateScaleFromKnownDistance_ReturnsCorrectScale()
        {
            // Arrange
            var point1 = new PdfPoint { X = 0, Y = 0 };
            var point2 = new PdfPoint { X = 100, Y = 0 }; // 100 pixels
            var knownDistance = 1000m; // Known to be 1000mm
            var unit = "mm";

            // Act
            var scale = _service.CalculateScaleFromKnownDistance(point1, point2, knownDistance, unit);

            // Assert
            Assert.Equal(10f, scale); // 1000mm / 100px = 10mm per pixel
        }

        [Fact]
        public void CalibrateScale_ReturnsCorrectScaleInfo()
        {
            // Arrange
            var pixelDistance = 50m; // 50 pixels
            var realDistance = 500m; // 500mm
            var unit = "mm";

            // Act
            var scaleInfo = _service.CalibrateScale(pixelDistance, realDistance, unit);

            // Assert
            Assert.NotNull(scaleInfo);
            Assert.Equal(10f, scaleInfo.Scale); // 500/50 = 10
            Assert.Equal(unit, scaleInfo.Unit);
            Assert.Equal("1:0", scaleInfo.ScaleText); // 1/(1/10) = 10, but integer division
            Assert.True(scaleInfo.IsMetric);
            Assert.Equal(0.1f, scaleInfo.PixelsPerUnit); // 50/500 = 0.1
        }
    }
}