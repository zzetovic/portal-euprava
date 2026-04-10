using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence;

public class PortalDbContext(DbContextOptions<PortalDbContext> options) : DbContext(options), IPortalDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<RequestTypeField> RequestTypeFields => Set<RequestTypeField>();
    public DbSet<RequestTypeAttachment> RequestTypeAttachments => Set<RequestTypeAttachment>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<SeupAktMapping> SeupAktMappings => Set<SeupAktMapping>();
    public DbSet<IntegrationOutbox> IntegrationOutbox => Set<IntegrationOutbox>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<Domain.Entities.FinanceSnapshot> FinanceSnapshots => Set<Domain.Entities.FinanceSnapshot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DeploymentBackup> DeploymentBackups => Set<DeploymentBackup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
