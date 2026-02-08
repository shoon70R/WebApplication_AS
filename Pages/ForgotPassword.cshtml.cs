using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
  public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
      private readonly AuthDbContext _db;
   private readonly IConfiguration _configuration;
        private readonly ILogger<ForgotPasswordModel> _logger;

   [BindProperty]
   public ForgotPassword Input { get; set; }

     public string StatusMessage { get; set; }
        public bool IsSuccessful { get; set; }

  public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
 IEmailService emailService,
  AuthDbContext db,
    IConfiguration configuration,
  ILogger<ForgotPasswordModel> logger)
  {
     _userManager = userManager;
        _emailService = emailService;
     _db = db;
    _configuration = configuration;
 _logger = logger;
    }

public void OnGet()
     {
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
     // Don't reveal whether user exists for security
     StatusMessage = "If an account exists with that email, you will receive password reset instructions.";
   IsSuccessful = true;
    return Page();
 }

       try
       {
         // Generate password reset token
    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        
// Build password reset link
    var resetLink = Url.Page("ResetPassword", pageHandler: null, values: new { email = user.Email, token = resetToken }, protocol: Request.Scheme);

 // Prepare email body
  var emailSubject = "Password Reset Request - Ace Job Agency";
    var emailBody = $@"
    <html>
       <head>
      <style>
      body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
    .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
    .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
     .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
 .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    .warning {{ color: #d9534f; font-weight: bold; }}
          </style>
      </head>
        <body>
 <div class=""container"">
  <div class=""header"">
              <h1>Password Reset Request</h1>
    </div>
         <div class=""content"">
   <p>Hello {user.FirstName} {user.LastName},</p>
        <p>We received a request to reset your password. Click the button below to reset your password:</p>
     <p><a href=""{resetLink}"" class=""button"">Reset Password</a></p>
             <p>Or copy and paste this link in your browser:</p>
       <p><small>{resetLink}</small></p>
      <p class=""warning"">?? This link will expire in 24 hours. If you did not request a password reset, please ignore this email.</p>
        <p>For security reasons, never share this link with anyone.</p>
        </div>
        <div class=""footer"">
       <p>&copy; 2024 Ace Job Agency. All rights reserved.</p>
     </div>
         </div>
   </body>
  </html>";

     // Send email
    var emailSent = await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

        if (!emailSent)
            {
    _logger.LogError($"Failed to send password reset email to {user.Email}");
   }

 // Log the password reset request
     _db.AuditLogs.Add(new AuditLog
     {
     UserId = user.Id,
     Action = "PasswordResetRequested",
    Timestamp = DateTime.UtcNow,
      IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
         });
     await _db.SaveChangesAsync();

 StatusMessage = "If an account exists with that email, you will receive password reset instructions.";
    IsSuccessful = true;
   Input = new ForgotPassword();
       return Page();
      }
  catch (Exception ex)
  {
     _logger.LogError($"Error processing password reset request: {ex.Message}");
         StatusMessage = "An error occurred while processing your request. Please try again later.";
     IsSuccessful = false;
   return Page();
  }
        }
    }
}
