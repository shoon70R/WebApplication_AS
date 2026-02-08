using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class ForgotPassword
    {
        [Required(ErrorMessage = "Email address is required")]
      [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}
