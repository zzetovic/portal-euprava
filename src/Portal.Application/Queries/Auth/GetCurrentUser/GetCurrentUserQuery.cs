using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Auth;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Auth.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserProfile>;

public class GetCurrentUserQueryHandler(IPortalDbContext db) : IRequestHandler<GetCurrentUserQuery, UserProfile>
{
    public async Task<UserProfile> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            throw new InvalidOperationException("USER_NOT_FOUND");

        return new UserProfile(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Oib: user.Oib,
            Phone: user.Phone,
            UserType: user.UserType.ToString(),
            PreferredLanguage: user.PreferredLanguage,
            MustChangePassword: user.MustChangePassword,
            EmailVerified: user.EmailVerifiedAt.HasValue);
    }
}
