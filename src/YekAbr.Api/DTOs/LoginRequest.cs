using System.ComponentModel.DataAnnotations;

namespace YekAbr.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "نام کاربری الزامی است.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    public string Password { get; set; } = string.Empty;
}
