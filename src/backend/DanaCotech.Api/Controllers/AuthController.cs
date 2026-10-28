using DanaCotech.Api.Security;
using DanaCotech.Application.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IPlatformService platformService, SessionTokenService tokenService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out _, out var currentRole))
            return Unauthorized(new { Success = false, Message = "A valid session is required" });
        if (!string.Equals(currentRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = "Admin access is required" });

        var response = await platformService.CreateUserAsync(request.Name, request.Email, request.Password, request.Role, cancellationToken);
        return response.Success && response.Data is not null
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        if (!TryGetAdmin(out _)) return AdminAccessDenied();
        return Ok(await platformService.GetUsersAsync(cancellationToken));
    }

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAdmin(out var actorUserId)) return AdminAccessDenied();
        var response = await platformService.UpdateUserRoleAsync(actorUserId, userId, request.Role, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await platformService.LoginAsync(request.Email, request.Password, cancellationToken);
        return response.Success && response.Data is not null
            ? Ok(new { response.Success, response.Message, Data = new { response.Data.UserId, response.Data.Name, response.Data.Email, response.Data.Role, Token = tokenService.Create(response.Data.UserId, response.Data.Role) } })
            : BadRequest(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var frontendUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5174";
        var response = await platformService.RegisterAsync(request.Name, request.Email, request.Password, $"{frontendUrl}/verify-email", cancellationToken);
        return response.Success ? Ok(new { response.Success, response.Message, Data = new { VerificationUrl = configuration.GetValue<bool>("PasswordReset:ExposeLinkInResponse") ? response.Data?.VerificationUrl : null, response.Data?.ExpiresAtUtc } }) : BadRequest(response);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await platformService.VerifyEmailAsync(request.Token, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var frontendUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5174";
        var response = await platformService.RequestPasswordResetAsync(request.Email, $"{frontendUrl}/reset-password", cancellationToken);
        var exposeLink = configuration.GetValue<bool>("PasswordReset:ExposeLinkInResponse");
        return Ok(new
        {
            response.Success,
            response.Message,
            Data = new
            {
                ResetUrl = exposeLink ? response.Data?.ResetUrl : null,
                response.Data?.ExpiresAtUtc
            }
        });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await platformService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private bool TryGetAdmin(out Guid userId) =>
        tokenService.TryValidate(Request.Headers.Authorization, out userId, out var role) &&
        string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);

    private IActionResult AdminAccessDenied() =>
        StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = "Admin access is required" });
}

public sealed record CreateUserRequest(string Name, string Email, string Password, string Role = UserRoles.Client);
public sealed record UpdateUserRoleRequest(string Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record VerifyEmailRequest(string Token);
