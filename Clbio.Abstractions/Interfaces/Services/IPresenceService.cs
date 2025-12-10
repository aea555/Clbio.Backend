namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IPresenceService
    {
        Task HeartbeatAsync(Guid userId);
        Task<List<Guid>> GetOnlineUsersAsync(IEnumerable<Guid> userIds);
    }
}