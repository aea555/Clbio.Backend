using AutoMapper;
using Clbio.Application.DTOs.V1.Board;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class BoardMappings : Profile
    {
        public BoardMappings()
        {
            CreateMap<CreateBoardDto, Board>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Workspace, opt => opt.Ignore())
                .ForMember(dest => dest.Columns, opt => opt.Ignore());

            CreateMap<UpdateBoardDto, Board>()
                .ForMember(dest => dest.WorkspaceId, opt => opt.Ignore())
                .ForMember(dest => dest.Workspace, opt => opt.Ignore())
                .ForMember(dest => dest.Columns, opt => opt.Ignore());

            CreateMap<Board, ReadBoardDto>()
                .ForMember(dest => dest.ColumnCount, opt => opt.MapFrom(src => src.Columns != null ? src.Columns.Count : 0));
        }
    }
}
