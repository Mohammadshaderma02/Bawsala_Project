using DanaCotech.Api.Security;
using DanaCotech.Application.Platform;

namespace DanaCotech.Api.Security;

public sealed class ActiveSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SessionTokenService tokenService, IPlatformRepository repository)
    {
        if (!HttpMethods.IsOptions(context.Request.Method) &&
            tokenService.TryValidate(context.Request.Headers.Authorization, out var userId, out var tokenRole))
        {
            var account = await repository.GetUserByIdAsync(userId, context.RequestAborted);
            if (account is null || account.IsDeleted || !account.Role.Equals(tokenRole, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { Success = false, Message = "This account is unavailable. Sign in again." }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}
