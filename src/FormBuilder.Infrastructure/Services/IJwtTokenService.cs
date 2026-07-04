using FormBuilder.Domain.Entities;

namespace FormBuilder.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
