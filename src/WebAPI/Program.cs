using Application;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

const string RulesGptCorsPolicy = nameof(RulesGptCorsPolicy);

string signingAuthority = builder.Configuration.GetValue<string>(nameof(signingAuthority));

builder.Services.AddAuthentication(options =>
{
    // Identity made Cookie authentication the default.
    // However, we want JWT Bearer Auth to be the default.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = signingAuthority;
    options.Audience = "ssw-rulesgpt";
    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            
            Console.WriteLine($"[AddJwtBearer.OnMessageReceived] path: {path}");
            Console.WriteLine($"[AddJwtBearer.OnMessageReceived] accessToken: {accessToken}");

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/ruleshub")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(RulesGptCorsPolicy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseCors(RulesGptCorsPolicy);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthRoutes();
app.MapHub<RulesHub>("/ruleshub");

app.Logger.LogInformation("Starting WebAPI");
app.Run();