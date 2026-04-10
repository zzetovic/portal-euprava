using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Auth.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangePasswordCommandHandler(
    IPortalDbContext db,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("USER_NOT_FOUND");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("INVALID_CURRENT_PASSWORD");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        await db.SaveChangesAsync(ct);
    }
}
