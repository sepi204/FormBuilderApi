using YekAbr.Domain.Entities;

namespace YekAbr.Infrastructure.Services;

public interface IAuthService
{
    Task<(bool Success, User? User, string? ErrorMessage, bool IsDuplicateUsername)> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<User?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
