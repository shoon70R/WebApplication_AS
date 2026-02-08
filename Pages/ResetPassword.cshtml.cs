using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class ResetPasswordModel : PageModel
    {
      private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordHistoryService _passwordHistoryService;
     private readonly AuthDbContext _db;
    private readonly ILogger<ResetPasswordModel> _logger;

     [BindProperty]
        public ResetPassword Input { get; set; }

  public string StatusMessage { get; set; }
 public bool IsSuccessful { get; set; }

 public ResetPasswordModel(
      UserManager<ApplicationUser> userManager,
    IPasswordHistoryService passwordHistoryService,
AuthDbContext db,
            ILogger<ResetPasswordModel> logger)
    {
  _userManager = userManager;
         _passwordHistoryService = passwordHistoryService;
    _db = db;
      _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string email, string token)
 {
          // Validate token and email from query parameters
  if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
       {
    ModelState.AddModelError(string.Empty, "Invalid password reset link.");
      return Page();
 }

            // Set email in the form
            Input = new ResetPassword { Email = email, Token = token };

      var user = await _userManager.FindByEmailAsync(email);
     if (user == null)
  {
     ModelState.AddModelError(string.Empty, "Invalid password reset link.");
        return Page();
  }

    return Page();
    }

        public async Task<IActionResult> OnPostAsync()
     {
 if (!ModelState.IsValid)
 {
        return Page();
     }

var user = await _userManager.FindByEmailAsync(Input.Email);
    if (user == null)
   {
          ModelState.AddModelError(string.Empty, "Invalid password reset link.");
  _db.AuditLogs.Add(new AuditLog
      {
 UserId = Input.Email,
       Action = "PasswordResetFailed-UserNotFound",
Timestamp = DateTime.UtcNow,
         IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
 });
       await _db.SaveChangesAsync();
  return Page();
            }

          try
 {
    // Check if new password matches any of the recent password history
    var isReused = await _passwordHistoryService.IsPasswordReusedAsync(user, Input.NewPassword);
        if (isReused)
       {
   ModelState.AddModelError("Input.NewPassword", "You cannot reuse one of your last 2 passwords. Please choose a different password.");
     _db.AuditLogs.Add(new AuditLog
     {
     UserId = user.Id,
  Action = "PasswordResetFailed-PasswordReused",
          Timestamp = DateTime.UtcNow,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
       });
  await _db.SaveChangesAsync();
    return Page();
   }

  // Reset the password using the token
    var resetResult = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);
 if (!resetResult.Succeeded)
{
            foreach (var error in resetResult.Errors)
  {
     ModelState.AddModelError(string.Empty, error.Description);
       }
          _db.AuditLogs.Add(new AuditLog
         {
   UserId = user.Id,
        Action = "PasswordResetFailed-InvalidToken",
    Timestamp = DateTime.UtcNow,
 IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
             });
          await _db.SaveChangesAsync();
return Page();
   }

     // Add old password to history
          await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash);

    // Update the last password changed timestamp
  await _passwordHistoryService.UpdateLastPasswordChangedAsync(user.Id);

     // Log successful password reset
 _db.AuditLogs.Add(new AuditLog
     {
           UserId = user.Id,
         Action = "PasswordResetSuccess",
 Timestamp = DateTime.UtcNow,
   IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
     });
       await _db.SaveChangesAsync();

 StatusMessage = "Your password has been reset successfully! You can now login with your new password.";
         IsSuccessful = true;
    Input = new ResetPassword();
return Page();
            }
    catch (Exception ex)
     {
_logger.LogError($"Error resetting password for user {user.Id}: {ex.Message}");
  ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
  return Page();
    }
     }
    }
}
