using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using RestaurantOS.Components;
using RestaurantOS.Data;
using RestaurantOS.Services.Accounts;
using RestaurantOS.Services.Auth;
using RestaurantOS.Services.Menu;
using RestaurantOS.Services.Orders;
using RestaurantOS.Services.Payments;
using RestaurantOS.Services.Reports;
using RestaurantOS.Services.Settings;

var builder = WebApplication.CreateBuilder(args);

// --- Blazor Server ---
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// --- Database ---
// Transient (not Scoped) so every component/service gets its own
// DbContext instance. Blazor Server components can have their
// OnInitializedAsync run close together (e.g. MainLayout + the routed
// page), and a single shared, scoped DbContext doesn't allow two
// operations on the same instance concurrently — that throws
// "A second operation was started on this context instance...".
// A fresh instance per injection avoids that entirely.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(3),
                errorNumbersToAdd: null)),
    contextLifetime: ServiceLifetime.Transient,
    optionsLifetime: ServiceLifetime.Singleton);

// --- Auth (custom, cookie-based) ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
// CustomAuthStateProvider must stay Scoped — it's the one shared source of
// truth for "who's logged in" across every component in the circuit.
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthenticationStateProviderAdapter>();
// AuthService itself is Transient (see note below) — it still reads/writes
// the one shared CustomAuthStateProvider above, so auth state stays
// consistent even though each caller gets its own AuthService instance.
builder.Services.AddTransient<IAuthService, AuthService>();

// --- App services ---
// Transient, not Scoped: each of these captures an ApplicationDbContext in
// its constructor. A Scoped service is built once per circuit and would
// keep reusing that one captured DbContext for the whole session — two
// components injecting the same Scoped service (e.g. App.razor and
// MainLayout both using ISettingsService) would then share one DbContext
// and hit the same concurrency exception the Transient DbContext change
// above is meant to fix. Transient services get a fresh instance (and thus
// a fresh DbContext) per injection site instead.
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IMenuService, MenuService>();
builder.Services.AddTransient<IAccountService, AccountService>();
builder.Services.AddTransient<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();

var app = builder.Build();

// --- Seed database on startup (dev convenience) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment())
    {
        try
        {
            _ = db.Permissions.Any();
        }
        catch (Exception ex) when (
            ex is Microsoft.Data.SqlClient.SqlException
            or Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException)
        {
            // With EnableRetryOnFailure() active, a failing connection surfaces
            // as RetryLimitExceededException (wrapping the real SqlException)
            // rather than the raw SqlException — catch both, since either one
            // here just means "no usable schema yet, rebuild it".
            db.Database.EnsureDeleted();

            // Dropping the database can leave stale pooled connections that
            // point at the now-deleted database — the next query on one of
            // those can fail with a confusing "no process on the other end
            // of the pipe" transient error. Clear the pool so the next
            // connection is opened fresh.
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
        }
    }

    DbInitializer.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
app.MapPost("/account/login", async (HttpContext ctx, IAuthService authService) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var (success, error) = await authService.LoginAsync(
        form["username"].ToString().Trim(), form["password"].ToString());

    if (success)
    {
        var returnUrl = form["returnUrl"].ToString();
        return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
    return Results.LocalRedirect($"/login?error={Uri.EscapeDataString(error ?? "Invalid login.")}");
});

app.MapPost("/account/logout", async (IAuthService authService) =>
{
    await authService.LogoutAsync();
    return Results.LocalRedirect("/login");
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();