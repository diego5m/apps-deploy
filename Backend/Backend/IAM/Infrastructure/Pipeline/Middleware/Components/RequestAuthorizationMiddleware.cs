using Backend.IAM.Application.Internal.OutboundServices;
using Backend.IAM.Domain.Model.Queries;
using Backend.IAM.Domain.Services;
using Backend.IAM.Infrastructure.Pipeline.Middleware.Attributes;

namespace Backend.IAM.Infrastructure.Pipeline.Middleware.Components;
/// <summary>
/// Request authorization middleware 
/// </summary>
/// <param name="next">
/// <see cref="RequestDelegate"/> Next middleware in pipeline
/// </param>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invoke middleware 
    /// </summary>
    /// <remarks>
    /// This middleware is responsible for authorizing requests. It checks if the request is allowed to be anonymous. If it is, it skips authorization. If it is not, it validates the token in the request header and retrieves the user associated with the token. It then updates the context with the user and continues to the next middleware in the pipeline.
    /// </remarks>
    /// <param name="context">
    /// <see cref="HttpContext"/> HTTP context
    /// </param>
    /// <param name="userQueryService">
    /// <see cref="IUserQueryService"/> User query service
    /// </param>
    /// <param name="tokenService">
    /// <see cref="ITokenService"/> Token service
    /// </param>
    /// <exception cref="Exception">
    /// Thrown when the token is null or invalid
    /// </exception>
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenService tokenService)
    {
        Console.WriteLine("Entering InvokeAsync");
        var endpoint = context.Request.HttpContext.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata?.Any(m => m.GetType() == typeof(AllowAnonymousAttribute)) ?? false;
        Console.WriteLine($"AllowAnonymous: {allowAnonymous}");
        if (allowAnonymous)
        {
            Console.WriteLine("Skipping authorization");
            await next(context);
            return;
        }
        Console.WriteLine("Entering authorization");

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("Token is missing or invalid");
            return;
        }
        var userId = await tokenService.ValidateToken(token);
        
        if (userId is null) throw new Exception("Invalid token");
        
        var getUserByIdQuery = new GetUserByIdQuery(userId.Value);

        var user = await userQueryService.Handle(getUserByIdQuery);
        Console.WriteLine("Successfully authorized. Updating context...");
        context.Items["User"] = user;
        Console.WriteLine("Continuing to next middleware in pipeline");
        await next(context);
    }
}