using Clbio.Application.DTOs.V1.Notification;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface INotificationAppService
    {
        Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
        Task SendNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default);
        // Get users notifications with pagination
        Task<Result<(List<ReadNotificationDto> Items, int TotalCount)>> GetMyNotificationsPagedAsync(
            Guid userId,
            int page,
            int pageSize,
            bool unreadOnly = false,
            CancellationToken ct = default);
        // Mark single notification of user as read
        Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
        // Mark all notifications of useras read
        Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
        // Delete notification
        Task<Result> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    }
}