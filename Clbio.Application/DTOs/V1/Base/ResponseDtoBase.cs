namespace Clbio.Application.DTOs.V1.Base
{
    public abstract class ResponseDtoBase : DtoBase
    {
        // correlation id for tracing for later
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // optional prop for response message
        public string? Message { get; set; }
    }
}
