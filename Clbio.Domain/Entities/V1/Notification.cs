using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Notification : EntityBase
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string MessageText { get; set; } = null!;
        public string Title { get; set; } = default!;
        public bool IsRead { get; set; } = false;
    }
}
