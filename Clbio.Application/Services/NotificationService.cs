using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities;

namespace Clbio.Application.Services
{
    public class NotificationService(IUnitOfWork unitOfWork)
    : ServiceBase<Notification>(unitOfWork), INotificationService
    {
    }
}
