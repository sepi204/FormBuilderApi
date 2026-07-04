using FormBuilder.Domain.Entities;
using FormBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Infrastructure.Services;

public class AuthService(AppDbContext dbContext) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<(bool Success, User? User, string? ErrorMessage, bool IsDuplicateUsername)> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();

        var usernameExists = await dbContext.Users
            .AnyAsync(user => user.Username == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            return (false, null, "نام کاربری قبلاً ثبت شده است.", true);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (true, user, null, false);
    }

    public async Task<User?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return verificationResult is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
