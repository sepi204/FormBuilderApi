namespace YekAbr.Api.DTOs;

public class UserResponse
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
