using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using WebApplication1.Model;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Register IHttpContextAccessor for accessing HttpContext via DI
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Distributed memory cache required for session
builder.Services.AddDistributedMemoryCache();

// Configure application cookie (session timeout)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // cookie/session timeout set to1 minute
    options.SlidingExpiration = true; // enforce strict inactivity timeout
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/StatusCode?statusCode=403";
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60); // session idle timeout (1 minute)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Data protection
builder.Services.AddDataProtection();
// Register IDataProtector-based EncryptionService with a defined purpose
builder.Services.AddScoped<IEncryptionService>(sp =>
{
    var provider = sp.GetRequiredService<IDataProtectionProvider>();
    var protector = provider.CreateProtector("WebApplication1.NricProtector");
    return new EncryptionService(protector);
});

// Register Input Sanitizer service for XSS and injection attack prevention
builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();

// Register Password Policy service for configurable password age and history settings
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

// Register Password History service for preventing password reuse and tracking password age
builder.Services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

// Register Email service for sending password reset emails
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Google reCAPTCHA v3 service
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

// Add antiforgery token service for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AntiforgeryToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Handle status code pages (404, 403, etc.)
app.UseStatusCodePagesWithReExecute("/StatusCode", "?statusCode={0}");

// Add security headers middleware
app.Use(async (context, next) =>
{
// Prevent clickjacking attacks
    context.Response.Headers["X-Frame-Options"] = "DENY";

// Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Enable XSS protection in browsers
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

    // Content Security Policy - Allows Google reCAPTCHA and development tools
    string csp = app.Environment.IsDevelopment()
     ? "default-src 'self'; script-src 'self' 'unsafe-inline' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' http://localhost:* ws://localhost:* wss://localhost:*; frame-src https://www.google.com/recaptcha/; object-src 'none';"
        : "default-src 'self'; script-src 'self' 'unsafe-inline' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-src https://www.google.com/recaptcha/; object-src 'none';";

    context.Response.Headers["Content-Security-Policy"] = csp;

    // Referrer Policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Permissions Policy
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// Enable session middleware
app.UseSession();

// Middleware to validate session id from claims and session store against user.CurrentSessionId and sign out if mismatch
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var passwordHistoryService = context.RequestServices.GetRequiredService<IPasswordHistoryService>();
        var user = await userManager.GetUserAsync(context.User);
   if (user != null)
 {
            // claim stored in auth cookie
      var claimSessionId = context.User.FindFirst("SessionId")?.Value;
            // session stored in server-side session store (distributed cache)
    var session = context.Session.GetString("UserSessionId");

            // If session expired (session value null) or claim missing or doesn't match user's current session, invalidate
       if (string.IsNullOrEmpty(session) || string.IsNullOrEmpty(claimSessionId) || user.CurrentSessionId != claimSessionId || session != claimSessionId)
            {
              // session invalid — sign out and redirect to login
              await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    context.Response.Redirect("/Login");
       return;
    }

     // Check if user must change password (maximum password age exceeded)
            var mustChangePassword = await passwordHistoryService.MustChangePasswordAsync(user);
     var currentPath = context.Request.Path.Value?.ToLower() ?? "";

            // If password must be changed and user is not already on the ChangePassword page, redirect them
            if (mustChangePassword && !currentPath.Contains("/changepassword") && !currentPath.Contains("/logout"))
{
 context.Response.Redirect("/ChangePassword");
    return;
    }
     }
    }
    await next();
});

app.UseAuthorization();

// Enable antiforgery token validation
app.UseAntiforgery();

app.MapRazorPages();

// Set default startup page: redirect to Index if authenticated, otherwise to Login
app.MapGet("/", async context =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Index");
    }
    else
    {
        context.Response.Redirect("/Login");
    }
});

app.Run();
