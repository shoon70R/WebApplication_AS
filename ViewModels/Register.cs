using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 128 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password does not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [StringLength(50, ErrorMessage = "Gender selection is invalid")]
        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Please select a valid gender option")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "NRIC is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "NRIC must be between 1 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "NRIC can only contain alphanumeric characters and hyphens")]
        public string Nric { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Resume (.pdf or .docx)")]
        public IFormFile Resume { get; set; }

        [StringLength(5000, ErrorMessage = "Bio cannot exceed 5000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Who Am I")]
        public string WhoAmI { get; set; }
    }
}
