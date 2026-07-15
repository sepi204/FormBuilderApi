using System.ComponentModel.DataAnnotations;

namespace YekAbr.Api.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "نام کاربری الزامی است.")]
    [MinLength(3, ErrorMessage = "نام کاربری باید حداقل ۳ کاراکتر باشد.")]
    [MaxLength(50, ErrorMessage = "نام کاربری نباید بیشتر از ۵۰ کاراکتر باشد.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [MinLength(6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد.")]
    [MaxLength(100, ErrorMessage = "رمز عبور نباید بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string Password { get; set; } = string.Empty;
}
