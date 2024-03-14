using System.Security.Claims;
using Application.Contracts;

namespace WebAPI.Services;

public class CurrentUserService : ICurrentUserService
{
    private const string EmailClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;
    
    public string? GetEmail()
        => _httpContextAccessor.HttpContext?.User.FindFirstValue(EmailClaimType);
}