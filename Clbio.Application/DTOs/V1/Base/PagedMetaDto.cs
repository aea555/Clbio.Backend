namespace Clbio.Application.DTOs.V1.Base

{
    public class PagedMetaDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool? UnreadOnly { get; set; } = false;
    }
}