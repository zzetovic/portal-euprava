using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Identity;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => PasswordHasher.Hash(password);
    public bool Verify(string password, string hash) => PasswordHasher.Verify(password, hash);
}
