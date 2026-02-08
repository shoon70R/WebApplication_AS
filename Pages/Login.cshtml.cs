using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _db;
        private readonly IRecaptchaService _recaptchaService;

        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }
        [BindProperty]
        public string? RecaptchaToken { get; set; }

        // Expose lockout end time to the Razor page so client-side JS can show a live countdown
        public DateTimeOffset? LockoutEnd { get; set; }

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthDbContext db, IRecaptchaService recaptchaService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _recaptchaService = recaptchaService;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("", "Enter credentials");
                return Page();
            }

            // Verify reCAPTCHA token
            if (string.IsNullOrEmpty(RecaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                _db.AuditLogs.Add(new AuditLog { UserId = Email ?? string.Empty, Action = "LoginFailed-MissingRecaptcha", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                await _db.SaveChangesAsync();
                return Page();
            }

            var recaptchaResult = await _recaptchaService.VerifyAsync(RecaptchaToken);
            if (!recaptchaResult.Success)
            {
                ModelState.AddModelError("", recaptchaResult.ErrorMessage ?? "reCAPTCHA verification failed. Please try again.");
                _db.AuditLogs.Add(new AuditLog { UserId = Email ?? string.Empty, Action = "LoginFailed-RecaptchaFailed", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                await _db.SaveChangesAsync();
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login");
                // audit failed login with unknown user - store attempted email to avoid null UserId
                _db.AuditLogs.Add(new AuditLog { UserId = Email ?? string.Empty, Action = "LoginFailed-UnknownUser", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                await _db.SaveChangesAsync();
                return Page();
            }

            // Check lockout status
            if (await _userManager.IsLockedOutAsync(user))
            {
                var end = await _userManager.GetLockoutEndDateAsync(user);
                LockoutEnd = end; // expose to view for client-side countdown
                // Don't add any error message for lockout - the view shows the countdown alert instead
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                // create a session id and store on user record
                var sessionId = Guid.NewGuid().ToString();
                user.CurrentSessionId = sessionId;
                await _userManager.UpdateAsync(user);

                // set server-side session variable
                HttpContext.Session.SetString("UserSessionId", sessionId);

                // set real user session values
                HttpContext.Session.SetString("StudentName", (user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty));
                HttpContext.Session.SetString("StudentId", user.Id ?? string.Empty);
                HttpContext.Session.SetString("Email", user.Email ?? string.Empty);
                if (user.DateOfBirth.HasValue)
                {
                    HttpContext.Session.SetString("DateOfBirth", user.DateOfBirth.Value.ToString("yyyy-MM-dd"));
                }

                // add claim for session id
                await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, new[] { new System.Security.Claims.Claim("SessionId", sessionId) });

                // audit log success
                _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LoginSuccess", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                await _db.SaveChangesAsync();

                return RedirectToPage("Index");
            }

            // If the result indicates the account is locked out, show lockout message
            if (result.IsLockedOut)
            {
                var end = await _userManager.GetLockoutEndDateAsync(user);
                LockoutEnd = end; // expose to view for client-side countdown
                _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LockedOut", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                await _db.SaveChangesAsync();
                // Don't add any error message for lockout - the view shows the countdown alert instead
                return Page();
            }

            // sign-in failed (not locked)
            // compute remaining attempts
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var max = _userManager.Options.Lockout.MaxFailedAccessAttempts;
            var attemptsRemaining = Math.Max(0, max - failedCount);

            ModelState.AddModelError("", "Invalid login.");
            _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LoginFailed", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
            await _db.SaveChangesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.CurrentSessionId = null;
                await _userManager.UpdateAsync(user);
            }
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

            // audit logout
            _db.AuditLogs.Add(new AuditLog { UserId = user?.Id, Action = "Logout", Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
            await _db.SaveChangesAsync();

            return RedirectToPage("Login");
        }
    }
}
