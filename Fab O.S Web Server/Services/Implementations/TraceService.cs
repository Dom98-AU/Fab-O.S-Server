using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations
{
    public class TraceService : ITraceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TraceService> _logger;
        private readonly ITenantService _tenantService;

        public TraceService(ApplicationDbContext context, ILogger<TraceService> logger, ITenantService tenantService)
        {
            _context = context;
            _logger = logger;
            _tenantService = tenantService;
        }

        public async Task<TraceRecord> CreateTraceRecordAsync(TraceRecord traceRecord)
        {
            try
            {
                traceRecord.TraceId = Guid.NewGuid();
                traceRecord.TraceNumber = await GenerateTraceNumberAsync();
                traceRecord.CreatedDate = DateTime.UtcNow;
                traceRecord.Status = TraceStatus.Active;
                traceRecord.CompanyId = _tenantService.GetCurrentCompanyId();
                traceRecord.UserId = _tenantService.GetCurrentUserId();

                _context.TraceRecords.Add(traceRecord);
                await _context.SaveChangesAsync();

                return traceRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating trace record: {ex.Message}");
                throw;
            }
        }

        public async Task<TraceRecord> GetTraceRecordAsync(Guid traceId)
        {
            return await _context.TraceRecords
                .Include(t => t.Materials)
                .Include(t => t.Documents)
                .Include(t => t.Processes)
                .FirstOrDefaultAsync(t => t.TraceId == traceId);
        }

        public async Task<List<TraceRecord>> GetTraceRecordsByEntityAsync(TraceableType entityType, int entityId)
        {
            return await _context.TraceRecords
                .Where(t => t.EntityType == entityType && t.EntityId == entityId)
                .ToListAsync();
        }

        public async Task<TraceRecord> UpdateTraceRecordAsync(TraceRecord traceRecord)
        {
            traceRecord.ModifiedDate = DateTime.UtcNow;
            _context.TraceRecords.Update(traceRecord);
            await _context.SaveChangesAsync();
            return traceRecord;
        }

        public async Task<bool> DeleteTraceRecordAsync(Guid traceId)
        {
            var record = await _context.TraceRecords.FirstOrDefaultAsync(t => t.TraceId == traceId);
            if (record == null) return false;

            _context.TraceRecords.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TraceMaterial> RecordMaterialReceiptAsync(TraceMaterial material)
        {
            material.CreatedDate = DateTime.UtcNow;
            _context.TraceMaterials.Add(material);
            await _context.SaveChangesAsync();
            return material;
        }

        public async Task<TraceMaterial> RecordMaterialIssueAsync(int traceRecordId, int catalogueItemId, decimal quantity, string unit)
        {
            // Get catalogue item for details
            var catalogueItem = await _context.CatalogueItems.FindAsync(catalogueItemId);
            if (catalogueItem == null)
            {
                throw new ArgumentException($"Catalogue item with ID {catalogueItemId} not found");
            }

            var material = new TraceMaterial
            {
                TraceRecordId = traceRecordId,
                CatalogueItemId = catalogueItemId,
                MaterialCode = catalogueItem.ItemCode,
                Description = catalogueItem.Description,
                Quantity = quantity,
                Unit = unit,
                CreatedDate = DateTime.UtcNow
            };

            _context.TraceMaterials.Add(material);
            await _context.SaveChangesAsync();
            return material;
        }

        public async Task<List<TraceMaterial>> GetMaterialsByTraceAsync(int traceRecordId)
        {
            return await _context.TraceMaterials
                .Where(m => m.TraceRecordId == traceRecordId)
                .Include(m => m.CatalogueItem)
                .ToListAsync();
        }

        public async Task<TraceMaterial> LinkMaterialToCatalogueAsync(int traceMaterialId, int catalogueItemId)
        {
            var material = await _context.TraceMaterials.FindAsync(traceMaterialId);
            if (material == null)
            {
                throw new ArgumentException($"TraceMaterial with ID {traceMaterialId} not found");
            }

            material.CatalogueItemId = catalogueItemId;
            await _context.SaveChangesAsync();
            return material;
        }

        public async Task<TraceProcess> RecordProcessStartAsync(int traceRecordId, int operationId, int? operatorId)
        {
            // Get operation details if available
            var operation = await _context.WorkOrderOperations.FindAsync(operationId);

            var process = new TraceProcess
            {
                TraceRecordId = traceRecordId,
                WorkOrderOperationId = operationId,
                OperatorId = operatorId,
                OperationCode = operation?.OperationCode ?? $"OP-{operationId}",
                OperationDescription = operation?.Description ?? "Manual Process",
                StartTime = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            };

            _context.TraceProcesses.Add(process);
            await _context.SaveChangesAsync();
            return process;
        }

        public async Task<TraceProcess> RecordProcessCompleteAsync(int traceProcessId, DateTime endTime, bool passedInspection)
        {
            var process = await _context.TraceProcesses.FindAsync(traceProcessId);
            if (process != null)
            {
                process.EndTime = endTime;
                process.PassedInspection = passedInspection;
                process.DurationMinutes = (decimal)(endTime - process.StartTime).TotalMinutes;
                await _context.SaveChangesAsync();
            }
            return process;
        }

        public async Task<TraceParameter> AddProcessParameterAsync(int traceProcessId, string name, string value, string unit)
        {
            var parameter = new TraceParameter
            {
                TraceProcessId = traceProcessId,
                ParameterName = name,
                ParameterValue = value,
                Unit = unit,
                CreatedDate = DateTime.UtcNow
            };

            _context.TraceParameters.Add(parameter);
            await _context.SaveChangesAsync();
            return parameter;
        }

        public async Task<List<TraceProcess>> GetProcessesByTraceAsync(int traceRecordId)
        {
            return await _context.TraceProcesses
                .Where(p => p.TraceRecordId == traceRecordId)
                .Include(p => p.Parameters)
                .ToListAsync();
        }

        public async Task<TraceAssembly> CreateAssemblyTraceAsync(TraceAssembly assembly)
        {
            assembly.CreatedDate = DateTime.UtcNow;
            _context.TraceAssemblies.Add(assembly);
            await _context.SaveChangesAsync();
            return assembly;
        }

        public async Task<TraceComponent> AddComponentToAssemblyAsync(int assemblyId, Guid componentTraceId, decimal quantity)
        {
            var component = new TraceComponent
            {
                TraceAssemblyId = assemblyId,
                ComponentTraceId = componentTraceId,
                QuantityUsed = quantity,
                Unit = "EA",
                CreatedDate = DateTime.UtcNow
            };

            _context.TraceComponents.Add(component);
            await _context.SaveChangesAsync();
            return component;
        }

        public async Task<List<TraceComponent>> GetAssemblyComponentsAsync(int assemblyId)
        {
            return await _context.TraceComponents
                .Where(c => c.TraceAssemblyId == assemblyId)
                .ToListAsync();
        }

        public async Task<TraceDocument> AttachDocumentAsync(int traceRecordId, TraceDocument document)
        {
            document.TraceRecordId = traceRecordId;
            document.UploadDate = DateTime.UtcNow;
            _context.TraceDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<List<TraceDocument>> GetDocumentsByTraceAsync(int traceRecordId)
        {
            return await _context.TraceDocuments
                .Where(d => d.TraceRecordId == traceRecordId)
                .ToListAsync();
        }

        public async Task<bool> VerifyDocumentAsync(int documentId, int verifiedByUserId)
        {
            var document = await _context.TraceDocuments.FindAsync(documentId);
            if (document != null)
            {
                document.IsVerified = true;
                document.VerifiedDate = DateTime.UtcNow;
                document.VerifiedBy = verifiedByUserId;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<TraceRecord>> TraceForwardAsync(Guid sourceTraceId)
        {
            var results = new List<TraceRecord>();
            await TraceForwardRecursiveAsync(sourceTraceId, results);
            return results;
        }

        private async Task TraceForwardRecursiveAsync(Guid parentTraceId, List<TraceRecord> results)
        {
            var children = await _context.TraceRecords
                .Where(t => t.ParentTraceId == parentTraceId)
                .ToListAsync();

            results.AddRange(children);

            foreach (var child in children)
            {
                await TraceForwardRecursiveAsync(child.TraceId, results);
            }
        }

        public async Task<List<TraceRecord>> TraceBackwardAsync(Guid targetTraceId)
        {
            var results = new List<TraceRecord>();
            var current = await _context.TraceRecords
                .FirstOrDefaultAsync(t => t.TraceId == targetTraceId);

            while (current != null && current.ParentTraceId.HasValue)
            {
                var parent = await _context.TraceRecords
                    .FirstOrDefaultAsync(t => t.TraceId == current.ParentTraceId.Value);
                if (parent != null)
                {
                    results.Add(parent);
                    current = parent;
                }
                else
                {
                    break;
                }
            }

            return results;
        }

        public async Task<List<TraceRecord>> GetTraceHierarchyAsync(Guid traceId)
        {
            var forward = await TraceForwardAsync(traceId);
            var backward = await TraceBackwardAsync(traceId);
            var current = await GetTraceRecordAsync(traceId);

            var hierarchy = new List<TraceRecord>();
            hierarchy.AddRange(backward);
            if (current != null) hierarchy.Add(current);
            hierarchy.AddRange(forward);

            return hierarchy;
        }

        public async Task<TraceReportDto> GenerateTraceReportAsync(Guid traceId)
        {
            var trace = await GetTraceRecordAsync(traceId);
            if (trace == null) return null;

            var materials = await GetMaterialsByTraceAsync(trace.Id);
            var processes = await GetProcessesByTraceAsync(trace.Id);
            var documents = await GetDocumentsByTraceAsync(trace.Id);

            var report = new TraceReportDto
            {
                TraceId = trace.TraceId,
                TraceNumber = trace.TraceNumber,
                GeneratedDate = DateTime.UtcNow,
                GeneratedBy = "System",
                Materials = materials.Select(m => new MaterialSummaryDto
                {
                    MaterialId = m.Id,
                    MaterialCode = m.MaterialCode,
                    MaterialDescription = m.Description,
                    Quantity = m.Quantity,
                    Unit = m.Unit,
                    BatchNumber = m.BatchNumber ?? string.Empty,
                    ReceivedDate = m.CreatedDate
                }).ToList(),
                Processes = processes.Select(p => new ProcessSummaryDto
                {
                    ProcessId = p.Id,
                    OperationName = p.OperationDescription,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    OperatorName = p.OperatorName ?? string.Empty,
                    PassedInspection = p.PassedInspection ?? false,
                    Parameters = p.Parameters.ToDictionary(
                        param => param.ParameterName,
                        param => param.ParameterValue)
                }).ToList(),
                Documents = documents.Select(d => new DocumentSummaryDto
                {
                    DocumentId = d.Id,
                    DocumentType = d.DocumentType.ToString(),
                    DocumentNumber = d.Id.ToString(),
                    Title = d.DocumentName,
                    UploadedDate = d.UploadDate,
                    IsVerified = d.IsVerified
                }).ToList(),
                Summary = $"Trace report for {trace.TraceNumber}"
            };

            return report;
        }

        public async Task<TraceTakeoff> CreateTakeoffFromPdfAsync(int traceRecordId, string pdfUrl, int? drawingId)
        {
            var takeoff = new TraceTakeoff
            {
                TraceRecordId = traceRecordId,
                PdfUrl = pdfUrl,
                DrawingId = drawingId,
                Status = "Draft",
                CreatedDate = DateTime.UtcNow,
                CompanyId = _tenantService.GetCurrentCompanyId()
            };

            _context.TraceTakeoffs.Add(takeoff);
            await _context.SaveChangesAsync();
            return takeoff;
        }

        public async Task<TraceTakeoffMeasurement> AddMeasurementAsync(int takeoffId, TraceTakeoffMeasurement measurement)
        {
            measurement.TraceTakeoffId = takeoffId;
            measurement.CreatedDate = DateTime.UtcNow;
            _context.TraceTakeoffMeasurements.Add(measurement);
            await _context.SaveChangesAsync();
            return measurement;
        }

        public async Task<List<TraceTakeoffMeasurement>> GetTakeoffMeasurementsAsync(int takeoffId)
        {
            return await _context.TraceTakeoffMeasurements
                .Where(m => m.TraceTakeoffId == takeoffId)
                .Include(m => m.CatalogueItem)
                .ToListAsync();
        }

        public async Task<TraceTakeoffMeasurement> LinkMeasurementToCatalogueAsync(int measurementId, int catalogueItemId)
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement != null)
            {
                measurement.CatalogueItemId = catalogueItemId;

                // Calculate weight if possible
                var catalogueItem = await _context.CatalogueItems.FindAsync(catalogueItemId);
                if (catalogueItem != null)
                {
                    measurement.CalculatedWeight = await CalculateWeightFromCatalogueAsync(
                        catalogueItemId, measurement.Value, measurement.Unit);
                }

                await _context.SaveChangesAsync();
            }
            return measurement;
        }

        public async Task<BillOfMaterialsDto> GenerateBOMFromTakeoffAsync(int takeoffId)
        {
            var measurements = await GetTakeoffMeasurementsAsync(takeoffId);
            var bom = new BillOfMaterialsDto
            {
                TakeoffId = takeoffId,
                GeneratedDate = DateTime.UtcNow,
                LineItems = new List<BOMLineItemDto>()
            };

            var groupedItems = measurements
                .Where(m => m.CatalogueItemId.HasValue)
                .GroupBy(m => m.CatalogueItemId.Value)
                .Select(g => new BOMLineItemDto
                {
                    CatalogueItemId = g.Key,
                    ItemCode = g.First().CatalogueItem?.ItemCode ?? string.Empty,
                    Description = g.First().CatalogueItem?.Description ?? string.Empty,
                    Quantity = g.Sum(m => m.Value),
                    Unit = g.First().Unit,
                    Weight = g.Sum(m => m.CalculatedWeight ?? 0)
                });

            bom.LineItems = groupedItems.ToList();
            bom.TotalWeight = bom.LineItems.Sum(i => i.Weight ?? 0);

            return bom;
        }

        public async Task<List<CatalogueItem>> SuggestCatalogueItemsAsync(string searchText, string category = null)
        {
            var query = _context.CatalogueItems.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(c =>
                    c.Description.Contains(searchText) ||
                    c.ItemCode.Contains(searchText) ||
                    c.Material.Contains(searchText));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.Category == category);
            }

            return await query.Take(20).ToListAsync();
        }

        public async Task<decimal> CalculateWeightFromCatalogueAsync(int catalogueItemId, decimal quantity, string unit)
        {
            var item = await _context.CatalogueItems.FindAsync(catalogueItemId);
            if (item == null) return 0;

            decimal weight = 0;

            // Calculate based on unit and available weight data
            if (unit.ToLower() == "m" && item.Mass_kg_m.HasValue)
            {
                weight = quantity * item.Mass_kg_m.Value;
            }
            else if (unit.ToLower() == "m2" && item.Mass_kg_m2.HasValue)
            {
                weight = quantity * item.Mass_kg_m2.Value;
            }
            else if (unit.ToLower() == "ea" && item.Weight_kg.HasValue)
            {
                weight = quantity * item.Weight_kg.Value;
            }

            return weight;
        }

        public async Task<MaterialRequirementDto> GenerateMaterialRequirementAsync(int traceRecordId)
        {
            var materials = await GetMaterialsByTraceAsync(traceRecordId);
            var requirement = new MaterialRequirementDto
            {
                TraceRecordId = traceRecordId,
                Requirements = new List<MaterialRequirement>()
            };

            foreach (var material in materials.Where(m => m.CatalogueItemId.HasValue))
            {
                var req = new MaterialRequirement
                {
                    CatalogueItemId = material.CatalogueItemId.Value,
                    ItemCode = material.CatalogueItem?.ItemCode,
                    Description = material.CatalogueItem?.Description ?? material.Description,
                    RequiredQuantity = material.Quantity,
                    Unit = material.Unit
                };

                requirement.Requirements.Add(req);
                requirement.TotalWeight += material.Weight ?? 0;
            }

            return requirement;
        }

        private async Task<string> GenerateTraceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.TraceRecords
                .CountAsync(t => t.CreatedDate.Year == year);

            return $"TRC-{year}-{(count + 1):D6}";
        }
    }
}