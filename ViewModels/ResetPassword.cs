using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
  public class ResetPassword
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
public string Email { get; set; }

        [Required(ErrorMessage = "Password reset token is required")]
        public string Token { get; set; }

   [Required(ErrorMessage = "New password is required")]
[DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 128 characters")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
   [Compare(nameof(NewPassword), ErrorMessage = "Password and confirmation password do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
    }
}
