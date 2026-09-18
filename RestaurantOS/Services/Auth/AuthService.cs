using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Auth;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<Employee?> GetCurrentEmployeeAsync();
    Task<bool> HasPermissionAsync(string permissionKey);
    Task<HashSet<string>> GetCurrentPermissionsAsync();
}

/// <summary>
/// Custom, cookie-based auth — no ASP.NET Identity. Passwords hashed
/// with BCrypt; permissions resolved per-request from Role -> RolePermission.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProviderAdapter adapter)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = adapter.Provider;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        var employee = await _db.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == username);

        if (employee is null || !employee.IsActive)
            return (false, "Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash))
            return (false, "Invalid username or password.");

        var permissions = await _db.RolePermissions
            .Where(rp => rp.RoleId == employee.RoleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission!.Key)
            .ToListAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.FullName),
            new("username", employee.Username),
            new(ClaimTypes.Role, employee.Role!.Name),
        };
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
        }

        _authStateProvider.NotifyUserAuthenticated(principal);

        employee.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _authStateProvider.NotifyUserLogout();
    }

    public async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var idClaim = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is null || !int.TryParse(idClaim, out var id)) return null;

        return await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<HashSet<string>> GetCurrentPermissionsAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindAll("perm").Select(c => c.Value).ToHashSet();
    }

    public async Task<bool> HasPermissionAsync(string permissionKey)
    {
        var perms = await GetCurrentPermissionsAsync();
        return perms.Contains(permissionKey);
    }
}

/// <summary>
/// Thin DI adapter so AuthService can depend on the concrete
/// CustomAuthStateProvider (needed for NotifyUserAuthenticated) while
/// the rest of the app depends on the abstract AuthenticationStateProvider.
/// </summary>
public class AuthenticationStateProviderAdapter
{
    public CustomAuthStateProvider Provider { get; }
    public AuthenticationStateProviderAdapter(CustomAuthStateProvider provider) => Provider = provider;
}
