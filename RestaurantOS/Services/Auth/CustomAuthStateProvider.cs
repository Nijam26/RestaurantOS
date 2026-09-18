using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RestaurantOS.Services.Auth;

/// <summary>
/// Lightweight in-memory auth state for Blazor Server circuits.
/// The real "source of truth" is the auth cookie issued at login
/// (see AuthService.LoginAsync) — this provider mirrors that into
/// the Blazor component tree so [Authorize] / AuthorizeView work.
/// Seeded from the incoming request's cookie so a freshly created
/// circuit (e.g. right after login's redirect) starts out already
/// authenticated instead of anonymous.
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser;

    public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = httpContextAccessor.HttpContext?.User as ClaimsPrincipal
            ?? new ClaimsPrincipal(new ClaimsIdentity());
    }

    public void NotifyUserAuthenticated(ClaimsPrincipal principal)
    {
        _currentUser = principal;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public void NotifyUserLogout()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));
}