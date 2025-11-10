using AutoMapper;
using Clbio.Application.DTOs.V1.Column;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class ColumnMappings : Profile
    {
        public ColumnMappings()
        {
            CreateMap<CreateColumnDto, Column>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Board, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());

            CreateMap<UpdateColumnDto, Column>()
                .ForMember(dest => dest.BoardId, opt => opt.Ignore())
                .ForMember(dest => dest.Board, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());

            CreateMap<Column, ReadColumnDto>()
                .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count));
        }
    }
}
