namespace Clbio.Application.DTOs.V1.Base
{
    public abstract class RequestDtoBase : DtoBase
    {
        // correlation id for tracing for later
        public string? CorrelationId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
