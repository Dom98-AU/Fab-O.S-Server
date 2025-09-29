using AutoMapper;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs;

namespace FabOS.WebServer.Mappings
{
    public class TraceMappingProfile : Profile
    {
        public TraceMappingProfile()
        {
            // TraceRecord mappings
            CreateMap<TraceRecord, TraceRecordDto>()
                .ForMember(dest => dest.Materials, opt => opt.MapFrom(src => src.Materials))
                .ForMember(dest => dest.Processes, opt => opt.MapFrom(src => src.Processes))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            CreateMap<CreateTraceRecordDto, TraceRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TraceId, opt => opt.Ignore())
                .ForMember(dest => dest.TraceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore());

            CreateMap<UpdateTraceRecordDto, TraceRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TraceId, opt => opt.Ignore())
                .ForMember(dest => dest.TraceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore());

            // TraceMaterial mappings
            CreateMap<TraceMaterial, TraceMaterialDto>()
                .ForMember(dest => dest.CatalogueItemCode, opt => opt.MapFrom(src => src.CatalogueItem.ItemCode))
                .ForMember(dest => dest.CatalogueItemDescription, opt => opt.MapFrom(src => src.CatalogueItem.Description));

            CreateMap<CreateTraceMaterialDto, TraceMaterial>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

            // TraceProcess mappings
            CreateMap<TraceProcess, TraceProcessDto>()
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src =>
                    src.EndTime.HasValue ? (src.EndTime.Value - src.StartTime).TotalMinutes : (double?)null));

            CreateMap<CreateTraceProcessDto, TraceProcess>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

            // TraceTakeoff mappings
            CreateMap<TraceTakeoff, TraceTakeoffDto>()
                .ForMember(dest => dest.MeasurementCount, opt => opt.MapFrom(src => src.Measurements.Count))
                .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src =>
                    src.Measurements.Where(m => m.CalculatedWeight.HasValue).Sum(m => m.CalculatedWeight.Value)));

            CreateMap<CreateTraceTakeoffDto, TraceTakeoff>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore());

            // TraceTakeoffMeasurement mappings
            CreateMap<TraceTakeoffMeasurement, TraceTakeoffMeasurementDto>()
                .ForMember(dest => dest.CatalogueItemCode, opt => opt.MapFrom(src => src.CatalogueItem.ItemCode))
                .ForMember(dest => dest.CatalogueItemDescription, opt => opt.MapFrom(src => src.CatalogueItem.Description));

            CreateMap<CreateTraceTakeoffMeasurementDto, TraceTakeoffMeasurement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

            CreateMap<UpdateTraceTakeoffMeasurementDto, TraceTakeoffMeasurement>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TraceTakeoffId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

            // CatalogueItem mappings
            CreateMap<CatalogueItem, CatalogueItemDto>();

            CreateMap<CatalogueItem, CatalogueItemSummaryDto>()
                .ForMember(dest => dest.WeightPerUnit, opt => opt.MapFrom(src =>
                    src.Mass_kg_m ?? src.Mass_kg_m2 ?? src.Weight_kg));

            // BOM mappings
            CreateMap<BillOfMaterialsDto, BillOfMaterials>().ReverseMap();
            CreateMap<BOMLineItem, BOMLineItemDto>().ReverseMap();

            // Report mappings
            CreateMap<TraceReportDto, TraceReport>().ReverseMap();
        }
    }
}