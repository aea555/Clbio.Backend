using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.Settings
{
    public class DatabaseSettings
    {
        [Required]
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
