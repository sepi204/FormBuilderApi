using System.Security.Claims;
using FormBuilder.Api.DTOs;
using FormBuilder.Domain.Entities;
using FormBuilder.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>
    /// ثبت‌نام کاربر جدید با نام کاربری و رمز عبور
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiMessageResponse
            {
                Message = GetFirstValidationMessage()
            });
        }

        var result = await authService.RegisterAsync(
            request.Username,
            request.Password,
            cancellationToken);

        if (result.IsDuplicateUsername)
        {
            return Conflict(new ApiMessageResponse { Message = result.ErrorMessage! });
        }

        if (!result.Success || result.User is null)
        {
            return BadRequest(new ApiMessageResponse
            {
                Message = result.ErrorMessage ?? "ثبت‌نام با خطا مواجه شد."
            });
        }

        var response = MapToUserResponse(result.User);

        return CreatedAtAction(nameof(Me), response);
    }

    /// <summary>
    /// ورود کاربر و دریافت توکن JWT
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiMessageResponse
            {
                Message = GetFirstValidationMessage()
            });
        }

        var user = await authService.ValidateCredentialsAsync(
            request.Username,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ApiMessageResponse
            {
                Message = "نام کاربری یا رمز عبور اشتباه است."
            });
        }

        var token = jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// دریافت اطلاعات کاربر احراز هویت‌شده
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new ApiMessageResponse
            {
                Message = "توکن نامعتبر است."
            });
        }

        var user = await authService.GetUserByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new ApiMessageResponse
            {
                Message = "کاربر یافت نشد."
            });
        }

        return Ok(MapToUserResponse(user));
    }

    private static UserResponse MapToUserResponse(User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAtUtc = user.CreatedAtUtc
        };

    private string GetFirstValidationMessage()
    {
        var firstError = ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault();

        return firstError ?? "داده‌های ارسالی معتبر نیستند.";
    }
}
