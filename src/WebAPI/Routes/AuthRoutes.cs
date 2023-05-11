using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Routes;

public static class AuthRoutes
{
    public static void MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags("Auth").WithOpenApi();

        group
            .MapGet(
                "/checkauth",
                Ok<string> (HttpContext context) =>
                {
                    var userName = context.User.Identity?.Name ?? throw new ArgumentNullException();
                    return TypedResults.Ok(userName);
                }
            )
            .WithName("CheckAuth");
    }
}
