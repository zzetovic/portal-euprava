using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Api.Cli;

public static class BootstrapTenantCommand
{
    public static RootCommand ConfigureBootstrapCommand(IServiceProvider services)
    {
        var rootCommand = new RootCommand("Portal eUprava CLI");

        var bootstrapCommand = new Command("bootstrap-tenant", "Create or verify a tenant with initial users");

        var codeOption = new Option<string>("--code", "Tenant code (slug)") { IsRequired = true };
        var nameOption = new Option<string>("--name", "Tenant display name") { IsRequired = true };
        var oibOption = new Option<string>("--oib", "Tenant OIB") { IsRequired = true };
        var adminEmailOption = new Option<string>("--admin-email", "Admin user email") { IsRequired = true };
        var adminFirstNameOption = new Option<string>("--admin-first-name", "Admin first name") { IsRequired = true };
        var adminLastNameOption = new Option<string>("--admin-last-name", "Admin last name") { IsRequired = true };
        var adminTempPasswordOption = new Option<string>("--admin-temp-password", "Admin temporary password") { IsRequired = true };
        var officerEmailOption = new Option<string?>("--officer-email", "Optional officer email");
        var officerFirstNameOption = new Option<string?>("--officer-first-name", "Officer first name");
        var officerLastNameOption = new Option<string?>("--officer-last-name", "Officer last name");
        var officerTempPasswordOption = new Option<string?>("--officer-temp-password", "Officer temporary password");
        var defaultProcessingDaysOption = new Option<int>("--default-processing-days", () => 5, "Default processing days");

        bootstrapCommand.AddOption(codeOption);
        bootstrapCommand.AddOption(nameOption);
        bootstrapCommand.AddOption(oibOption);
        bootstrapCommand.AddOption(adminEmailOption);
        bootstrapCommand.AddOption(adminFirstNameOption);
        bootstrapCommand.AddOption(adminLastNameOption);
        bootstrapCommand.AddOption(adminTempPasswordOption);
        bootstrapCommand.AddOption(officerEmailOption);
        bootstrapCommand.AddOption(officerFirstNameOption);
        bootstrapCommand.AddOption(officerLastNameOption);
        bootstrapCommand.AddOption(officerTempPasswordOption);
        bootstrapCommand.AddOption(defaultProcessingDaysOption);

        bootstrapCommand.SetHandler(async (context) =>
        {
            var code = context.ParseResult.GetValueForOption(codeOption)!;
            var name = context.ParseResult.GetValueForOption(nameOption)!;
            var oib = context.ParseResult.GetValueForOption(oibOption)!;
            var adminEmail = context.ParseResult.GetValueForOption(adminEmailOption)!;
            var adminFirstName = context.ParseResult.GetValueForOption(adminFirstNameOption)!;
            var adminLastName = context.ParseResult.GetValueForOption(adminLastNameOption)!;
            var adminTempPassword = context.ParseResult.GetValueForOption(adminTempPasswordOption)!;
            var officerEmail = context.ParseResult.GetValueForOption(officerEmailOption);
            var officerFirstName = context.ParseResult.GetValueForOption(officerFirstNameOption);
            var officerLastName = context.ParseResult.GetValueForOption(officerLastNameOption);
            var officerTempPassword = context.ParseResult.GetValueForOption(officerTempPasswordOption);
            var defaultProcessingDays = context.ParseResult.GetValueForOption(defaultProcessingDaysOption);

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            await ExecuteAsync(db, passwordHasher, new BootstrapOptions(
                code, name, oib,
                adminEmail, adminFirstName, adminLastName, adminTempPassword,
                officerEmail, officerFirstName, officerLastName, officerTempPassword,
                defaultProcessingDays));
        });

        rootCommand.AddCommand(bootstrapCommand);
        return rootCommand;
    }

    public record BootstrapOptions(
        string Code, string Name, string Oib,
        string AdminEmail, string AdminFirstName, string AdminLastName, string AdminTempPassword,
        string? OfficerEmail, string? OfficerFirstName, string? OfficerLastName, string? OfficerTempPassword,
        int DefaultProcessingDays);

    public static async Task ExecuteAsync(IPortalDbContext db, IPasswordHasher passwordHasher, BootstrapOptions options)
    {
        // Check if tenant already exists (idempotent)
        var existingTenant = await db.Tenants.FirstOrDefaultAsync(t => t.Code == options.Code);

        if (existingTenant is not null)
        {
            Console.WriteLine($"Tenant '{options.Code}' already exists (ID: {existingTenant.Id}).");
            var existingUsers = await db.Users
                .Where(u => u.TenantId == existingTenant.Id)
                .Select(u => new { u.Email, u.UserType })
                .ToListAsync();

            foreach (var u in existingUsers)
                Console.WriteLine($"  User: {u.Email} ({u.UserType})");

            Console.WriteLine("No changes made (idempotent).");
            return;
        }

        // Create tenant
        var settingsJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            default_processing_days = options.DefaultProcessingDays,
            mail_from = ""
        });

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Code = options.Code,
            Name = options.Name,
            Oib = options.Oib,
            Settings = settingsJson,
            IsActive = true
        };
        db.Tenants.Add(tenant);

        // Create admin user
        var adminUser = CreateUser(tenant.Id, options.AdminEmail, options.AdminFirstName,
            options.AdminLastName, options.AdminTempPassword, UserType.JlsAdmin, passwordHasher);
        db.Users.Add(adminUser);

        // Audit log for tenant
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = null,
            Action = "bootstrap.tenant_created",
            EntityType = "tenant",
            EntityId = tenant.Id,
            After = System.Text.Json.JsonSerializer.Serialize(new { tenant.Code, tenant.Name, tenant.Oib })
        });

        // Audit log for admin user
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = null,
            Action = "bootstrap.user_created",
            EntityType = "user",
            EntityId = adminUser.Id,
            After = System.Text.Json.JsonSerializer.Serialize(new { adminUser.Email, UserType = adminUser.UserType.ToString() })
        });

        // Optionally create officer
        User? officerUser = null;
        if (!string.IsNullOrEmpty(options.OfficerEmail))
        {
            var officerPassword = options.OfficerTempPassword ?? options.AdminTempPassword;
            officerUser = CreateUser(tenant.Id, options.OfficerEmail,
                options.OfficerFirstName ?? "Officer", options.OfficerLastName ?? "",
                officerPassword, UserType.JlsOfficer, passwordHasher);
            db.Users.Add(officerUser);

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = null,
                Action = "bootstrap.user_created",
                EntityType = "user",
                EntityId = officerUser.Id,
                After = System.Text.Json.JsonSerializer.Serialize(new { officerUser.Email, UserType = officerUser.UserType.ToString() })
            });
        }

        await db.SaveChangesAsync(CancellationToken.None);

        // Output results
        Console.WriteLine("=== Bootstrap Complete ===");
        Console.WriteLine($"Tenant ID:   {tenant.Id}");
        Console.WriteLine($"Tenant Code: {tenant.Code}");
        Console.WriteLine($"Tenant Name: {tenant.Name}");
        Console.WriteLine();
        Console.WriteLine("--- Admin User ---");
        Console.WriteLine($"  Email:          {adminUser.Email}");
        Console.WriteLine($"  Temp Password:  {options.AdminTempPassword}");
        Console.WriteLine($"  (Must change password on first login)");

        if (officerUser is not null)
        {
            Console.WriteLine();
            Console.WriteLine("--- Officer User ---");
            Console.WriteLine($"  Email:          {officerUser.Email}");
            Console.WriteLine($"  Temp Password:  {options.OfficerTempPassword ?? options.AdminTempPassword}");
            Console.WriteLine($"  (Must change password on first login)");
        }

        Console.WriteLine();
        Console.WriteLine("IMPORTANT: Record these credentials now. They will not be shown again.");
    }

    private static User CreateUser(
        Guid tenantId, string email, string firstName, string lastName,
        string tempPassword, UserType userType, IPasswordHasher passwordHasher)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHasher.Hash(tempPassword),
            FirstName = firstName,
            LastName = lastName,
            UserType = userType,
            IsActive = true,
            MustChangePassword = true,
            EmailVerifiedAt = DateTime.UtcNow, // Bootstrap users don't need email verification
            PreferredLanguage = "hr"
        };
    }
}
