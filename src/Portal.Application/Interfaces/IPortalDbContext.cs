using Microsoft.EntityFrameworkCore;
using Portal.Domain.Entities;

namespace Portal.Application.Interfaces;

public interface IPortalDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<RequestType> RequestTypes { get; }
    DbSet<RequestTypeField> RequestTypeFields { get; }
    DbSet<RequestTypeAttachment> RequestTypeAttachments { get; }
    DbSet<Request> Requests { get; }
    DbSet<RequestAttachment> RequestAttachments { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
    DbSet<SeupAktMapping> SeupAktMappings { get; }
    DbSet<IntegrationOutbox> IntegrationOutbox { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<Portal.Domain.Entities.FinanceSnapshot> FinanceSnapshots { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DeploymentBackup> DeploymentBackups { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
