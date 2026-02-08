using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class ChangePasswordModel : PageModel
    {
  private readonly UserManager<ApplicationUser> _userManager;
     private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthDbContext _db;
        private readonly IPasswordHistoryService _passwordHistoryService;

        [BindProperty]
   public ChangePassword Input { get; set; }

        public string StatusMessage { get; set; }
        public bool IsSuccessful { get; set; }

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
   AuthDbContext db,
     IPasswordHistoryService passwordHistoryService)
        {
       _userManager = userManager;
 _signInManager = signInManager;
 _db = db;
       _passwordHistoryService = passwordHistoryService;
     }

      public async Task<IActionResult> OnGetAsync()
   {
        // Check if user is authenticated
  if (!User.Identity.IsAuthenticated)
 {
 return RedirectToPage("Login");
 }

var user = await _userManager.GetUserAsync(User);
   if (user == null)
         {
 return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'. Please call support.");
 }

     return Page();
   }

        public async Task<IActionResult> OnPostAsync()
        {
if (!User.Identity.IsAuthenticated)
       {
       return RedirectToPage("Login");
       }

    if (!ModelState.IsValid)
   {
      return Page();
  }

        var user = await _userManager.GetUserAsync(User);
 if (user == null)
    {
   return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'. Please call support.");
   }

     // Check if user must change password (max password age exceeded)
   var mustChangePassword = await _passwordHistoryService.MustChangePasswordAsync(user);

    // Check if user can change password (minimum password age not exceeded)
 var (canChangePassword, minutesUntilCanChange) = await _passwordHistoryService.CanChangePasswordAsync(user);
    if (!canChangePassword && !mustChangePassword)
    {
       StatusMessage = $"You must wait {minutesUntilCanChange} minute(s) before changing your password again.";
     IsSuccessful = false;
       ModelState.AddModelError(string.Empty, StatusMessage);
 return Page();
    }

  // Verify current password
      var verifyPasswordResult = await _userManager.CheckPasswordAsync(user, Input.CurrentPassword);
     if (!verifyPasswordResult)
 {
    ModelState.AddModelError("Input.CurrentPassword", "Current password is incorrect.");
    _db.AuditLogs.Add(new AuditLog
     {
UserId = user.Id,
         Action = "ChangePasswordFailed-IncorrectPassword",
       Timestamp = DateTime.UtcNow,
      IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
         });
     await _db.SaveChangesAsync();
     return Page();
   }

    // Check if new password is the same as current password
     if (Input.CurrentPassword == Input.NewPassword)
  {
    ModelState.AddModelError("Input.NewPassword", "New password must be different from the current password.");
     return Page();
     }

   // Check if new password matches any of the recent password history (last 2 passwords)
          var isReused = await _passwordHistoryService.IsPasswordReusedAsync(user, Input.NewPassword);
     if (isReused)
      {
    ModelState.AddModelError("Input.NewPassword", "You cannot reuse one of your last 2 passwords. Please choose a different password.");
     _db.AuditLogs.Add(new AuditLog
        {
           UserId = user.Id,
      Action = "ChangePasswordFailed-PasswordReused",
       Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
       });
  await _db.SaveChangesAsync();
 return Page();
    }

      // Change password
     var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
       if (!changePasswordResult.Succeeded)
   {
     foreach (var error in changePasswordResult.Errors)
      {
    ModelState.AddModelError(string.Empty, error.Description);
 }
     _db.AuditLogs.Add(new AuditLog
      {
   UserId = user.Id,
     Action = "ChangePasswordFailed-ValidationError",
     Timestamp = DateTime.UtcNow,
   IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
       });
    await _db.SaveChangesAsync();
      return Page();
     }

            // Add the old password to history before the change
     await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash);

    // Update the last password changed timestamp
  await _passwordHistoryService.UpdateLastPasswordChangedAsync(user.Id);

        // Force user to re-authenticate with new password
  await _signInManager.RefreshSignInAsync(user);

     // Log successful password change
     _db.AuditLogs.Add(new AuditLog
     {
     UserId = user.Id,
  Action = "ChangePasswordSuccess",
     Timestamp = DateTime.UtcNow,
       IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
 });
     await _db.SaveChangesAsync();

    StatusMessage = "Your password has been changed successfully!";
 IsSuccessful = true;

 // Clear the form
    Input = new ChangePassword();

    // Return page so success message is displayed (JavaScript will handle redirect)
   return Page();
        }
    }
}
