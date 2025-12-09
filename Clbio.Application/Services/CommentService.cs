using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.Comment;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class CommentService(
        IUnitOfWork uow,
        IMapper mapper,
        ICacheInvalidationService invalidator,
        ILogger<CommentService>? logger = null)
        : ServiceBase<Comment>(uow, logger), ICommentAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();
        private readonly IRepository<Comment> _commentRepo = uow.Repository<Comment>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<WorkspaceMember> _memberRepo = uow.Repository<WorkspaceMember>();

        // ---------------------------------------------------------------------
        // GET ALL
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadCommentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. Check hierarchy of Task
                // Task -> Column -> Board -> Workspace chain
                var task = await _taskRepo.Query()
                    .Include(t => t.Column)
                        .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct) ?? throw new InvalidOperationException("Task not found.");

                // security check
                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Task does not belong to the specified workspace.");

                // 2. fetch comments
                var comments = await _commentRepo.Query()
                    .Where(c => c.TaskId == taskId)
                    .Include(c => c.Author)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync(ct);

                return _mapper.Map<List<ReadCommentDto>>(comments);

            }, _logger, "COMMENT_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------------
        public async Task<Result<ReadCommentDto>> CreateAsync(Guid workspaceId, CreateCommentDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. Get task and its hierarchy
                var task = await _taskRepo.Query()
                    .Include(t => t.Column)
                        .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == dto.TaskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                // Security check: Parent-Child Consistency
                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Cannot add comment: Task is outside the workspace scope.");

                // 2. Entity oluştur
                var comment = _mapper.Map<Comment>(dto);

                await _commentRepo.AddAsync(comment, ct);
                await _uow.SaveChangesAsync(ct);

                // Yazar bilgisini (Avatar, İsim) dönmek için kullanıcıyı yüklememiz veya include etmemiz lazım.
                // EF Core Add sonrası navigation property'i otomatik doldurmayabilir, explicit load yapıyoruz.
                await _uow.Repository<User>().GetByIdAsync(dto.AuthorId, false, ct); // Context'e track ettirmek için

                // Veya daha temiz: DTO mapping öncesi manuel setleme
                // (ReadDto mapping'i Author navigation property'sine bağlı olduğu için bu adım önemli)

                // Cache Invalidasyonu (Opsiyonel: Yorumlar genellikle anlık çekilir ama Workspace versiyonunu artırmak 
                // "Last Activity" gibi özellikler için iyidir)
                await _invalidator.InvalidateWorkspace(workspaceId);

                // Mapping için Author'ı tekrar yükleyelim (Garanti yöntem)
                var createdComment = await _commentRepo.Query()
                    .Include(c => c.Author)
                    .FirstAsync(c => c.Id == comment.Id, ct);

                return _mapper.Map<ReadCommentDto>(createdComment);

            }, _logger, "COMMENT_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------------
        public async Task<Result> DeleteAsync(Guid workspaceId, Guid commentId, Guid currentUserId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. Yorumu ve gerekli hiyerarşiyi çek
                var comment = await _commentRepo.Query()
                    .Include(c => c.Task)
                        .ThenInclude(t => t.Column)
                            .ThenInclude(col => col.Board)
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct)
                    ?? throw new InvalidOperationException("Comment not found.");

                // 2. Temel Güvenlik: Yorum bu workspace'e mi ait?
                if (comment.Task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Comment is outside the workspace scope.");

                // 3. İşlemi yapan kullanıcıyı (Current User) çek
                var currentUser = await _userRepo.GetByIdAsync(currentUserId, false, ct)
                                  ?? throw new InvalidOperationException("User not found.");

                // --- YETKİ KONTROL MATRİSİ ---

                // KURAL 1: Global Admin her şeyi silebilir.
                if (currentUser.GlobalRole == GlobalRole.Admin)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                    return;
                }

                // KURAL 2: Kullanıcı kendi yorumunu her zaman silebilir.
                if (comment.AuthorId == currentUserId)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                    return;
                }

                // Buraya geldiysek kullanıcı BAŞKASININ yorumunu silmeye çalışıyor.
                // Şimdi rütbeleri savaştıracağız.

                // A. İşlemi yapan kişinin Workspace'teki rütbesini bul
                var currentMember = await _memberRepo.Query()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, ct);

                if (currentMember == null)
                    throw new UnauthorizedAccessException("You are not a member of this workspace.");

                // B. Yorum sahibinin (Target) Workspace'teki rütbesini bul
                var authorMember = await _memberRepo.Query()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == comment.AuthorId, ct);

                // Yorum sahibi artık workspace üyesi değilse (ayrılmışsa), 
                // genellikle PrivilegedMember ve üstünün silmesine izin verilir.
                // Eğer üye ise gerçek rütbesini, değilse en düşük rütbeden bile düşük (-1) varsayalım.
                int authorRoleValue = authorMember != null ? (int)authorMember.Role : -1;
                int currentRoleValue = (int)currentMember.Role;

                // KURAL 3: Hiyerarşi Kontrolü
                // Eğer benim rütbem, hedef kişinin rütbesinden BÜYÜKSE silebilirim.
                // Eşitse SİLEMEM (Privileged vs Privileged).
                // Küçükse zaten SİLEMEM.

                // Örnek:
                // Owner(2) > Privileged(1) -> TRUE (Silebilir)
                // Privileged(1) > Member(0) -> TRUE (Silebilir)
                // Privileged(1) > Privileged(1) -> FALSE (Silemez)
                // Member(0) > Member(0) -> FALSE (Silemez)

                if (currentRoleValue > authorRoleValue)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                }
                else
                {
                    throw new UnauthorizedAccessException("You do not have sufficient permissions to delete this user's comment.");
                }

            }, _logger, "COMMENT_DELETE_FAILED");
        }

        private async Task PerformDelete(Guid workspaceId, Guid commentId, CancellationToken ct)
        {
            await _commentRepo.DeleteAsync(commentId, ct);
            await _uow.SaveChangesAsync(ct);
            await _invalidator.InvalidateWorkspace(workspaceId);
        }
    }
}