using YekAbr.Domain.Entities;

namespace YekAbr.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
