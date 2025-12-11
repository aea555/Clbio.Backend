using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Clbio.Application.DTOs.V1.Base
{
    public abstract class RequestDtoBase : DtoBase
    {
        // correlation id for tracing for later
        [StringLength(100)]
        [JsonIgnore]
        public string? CorrelationId { get; set; }
        [JsonIgnore]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
