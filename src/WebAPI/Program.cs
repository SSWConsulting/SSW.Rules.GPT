using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

const string RulesGptCorsPolicy = nameof(RulesGptCorsPolicy);

//var connectionString = builder.Configuration.GetConnectionString("LocalConnection2") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlite(connectionString));
//
//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
//
//builder.Services.AddIdentityServer()
//    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

string signingAuthority = builder.Configuration.GetValue<string>(nameof(signingAuthority));
Console.WriteLine(signingAuthority);

builder.Services.AddAuthentication(options =>
{
    // Identity made Cookie authentication the default.
    // However, we want JWT Bearer Auth to be the default.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = signingAuthority;
    //options.Audience = "rewards";
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

//builder.Services
//    .AddAuthentication()
//    .AddIdentityServerJwt();

//builder.Services.TryAddEnumerable(
//    ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>,
//        ConfigureJwtBearerOptions>());

//builder.Services.AddSignalR();

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

//app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthRoutes();
app.MapHub<RulesHub>("/ruleshub");

app.Logger.LogInformation("Starting WebAPI");
app.Run();