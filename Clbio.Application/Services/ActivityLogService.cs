using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Services
{
    public class ActivityLogService(IUnitOfWork unitOfWork)
    : ServiceBase<ActivityLog>(unitOfWork), IActivityLogService
    {
    }
}
