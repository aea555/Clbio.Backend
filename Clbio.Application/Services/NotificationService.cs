using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache; // Notification count cache'i için
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Notification;
using Clbio.Application.Extensions; // CacheKeys
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class NotificationService(
        IUnitOfWork uow,
        IMapper mapper,
        ISocketService socketService,
        ICacheService cache,
        ILogger<NotificationService>? logger = null)
        : ServiceBase<Notification>(uow, logger), INotificationAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ISocketService _socketService = socketService;
        private readonly ICacheService _cache = cache;
        private readonly IRepository<Notification> _notifRepo = uow.Repository<Notification>();

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var key = CacheKeys.NotificationCount(userId);

                var count = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        return await _notifRepo.Query()
                            .AsNoTracking()
                            .Where(n => n.UserId == userId && !n.IsRead)
                            .CountAsync(ct);
                    },
                    TimeSpan.FromHours(2));

                return count;
            }, _logger, "NOTIF_COUNT_FAILED");
        }

        public async Task SendNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    MessageText = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notifRepo.AddAsync(notification, ct);
                await _uow.SaveChangesAsync(ct);

                await _cache.RemoveAsync(CacheKeys.NotificationCount(userId));

                var dto = _mapper.Map<ReadNotificationDto>(notification);
                await _socketService.SendToUserAsync(userId, "NotificationReceived", dto, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        public async Task<Result<(List<ReadNotificationDto> Items, int TotalCount)>> GetMyNotificationsPagedAsync(
            Guid userId,
            int page,
            int pageSize,
            bool unreadOnly = false,
            CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. prepare query
                var query = _notifRepo.Query()
                    .AsNoTracking()
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                    query = query.Where(n => !n.IsRead);

                // 2. get total count, required for pagination metadata
                var totalCount = await query.CountAsync(ct);

                // 3. fetch data, descending order
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var dtos = _mapper.Map<List<ReadNotificationDto>>(notifications);

                return (dtos, totalCount);
            }, _logger, "NOTIF_LIST_FAILED");
        }

        public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var notification = await _notifRepo.GetByIdAsync(notificationId, true, ct)
                                   ?? throw new InvalidOperationException("Notification not found.");

                // safety: can't mark others' notifications as read
                if (notification.UserId != userId)
                    throw new UnauthorizedAccessException("This notification does not belong to you.");

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    await _uow.SaveChangesAsync(ct);

                    // clean/update unread not. in cache
                    await _cache.RemoveAsync(CacheKeys.NotificationCount(userId));
                }
            }, _logger, "NOTIF_MARK_READ_FAILED");
        }

        public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // Batch update 
                await _notifRepo.Query()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

                await _cache.RemoveAsync(CacheKeys.NotificationCount(userId));
            }, _logger, "NOTIF_MARK_ALL_FAILED");
        }

        public async Task<Result> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var notification = await _notifRepo.GetByIdAsync(notificationId, false, ct);
                if (notification == null) return;

                if (notification.UserId != userId)
                    throw new UnauthorizedAccessException("Access denied.");

                await _notifRepo.DeleteAsync(notificationId, ct);
                await _uow.SaveChangesAsync(ct);

                await _cache.RemoveAsync(CacheKeys.NotificationCount(userId));
            }, _logger, "NOTIF_DELETE_FAILED");
        }
    }
}