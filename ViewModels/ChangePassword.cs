using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
  public class ChangePassword
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "New password must be between 12 and 128 characters")]
        [Display(Name = "New Password")]
      public string NewPassword { get; set; }

   [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
  [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
    }
}
