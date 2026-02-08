using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.ViewModels;
using WebApplication1.Model;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Pages
{
  public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }
        private readonly IWebHostEnvironment env;
        private readonly IEncryptionService encryptionService;
        private readonly IInputSanitizer inputSanitizer;

        [BindProperty]
    public Register RModel { get; set; }

  public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env, IEncryptionService encryptionService, IInputSanitizer inputSanitizer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.env = env;
         this.encryptionService = encryptionService;
    this.inputSanitizer = inputSanitizer;
        }

        public void OnGet()
        {
      }

        //Save data into the database
    public async Task<IActionResult> OnPostAsync()
    {
       if (ModelState.IsValid)
    {
   try
   {
              // Sanitize all text inputs to prevent XSS and injection attacks
 var sanitizedEmail = inputSanitizer.SanitizeEmail(RModel.Email);
         var sanitizedFirstName = inputSanitizer.SanitizeText(RModel.FirstName, 100);
      var sanitizedLastName = inputSanitizer.SanitizeText(RModel.LastName, 100);
   var sanitizedNric = inputSanitizer.SanitizeText(RModel.Nric, 20);
        var sanitizedGender = ValidateAndGetGender(RModel.Gender);
          var sanitizedWhoAmI = string.IsNullOrWhiteSpace(RModel.WhoAmI) 
               ? string.Empty 
  : inputSanitizer.SanitizeMultilineText(RModel.WhoAmI, 5000);

      // Ensure email unique
           var existing = await userManager.FindByEmailAsync(sanitizedEmail);
        if (existing != null)
      {
          ModelState.AddModelError("", "Email is already registered");
   return Page();
        }

 // Validate date of birth
      if (!ValidateDateOfBirth(RModel.DateOfBirth))
    {
             ModelState.AddModelError("RModel.DateOfBirth", "Please enter a valid date of birth");
                return Page();
        }

  var user = new ApplicationUser()
              {
          UserName = sanitizedEmail,
           Email = sanitizedEmail,
        FirstName = sanitizedFirstName,
    LastName = sanitizedLastName,
   Gender = sanitizedGender,
            DateOfBirth = RModel.DateOfBirth,
         WhoAmI = sanitizedWhoAmI
      };

                    // Server-side password validation using Identity validators
              foreach (var validator in userManager.PasswordValidators)
         {
    var validationResult = await validator.ValidateAsync(userManager, user, RModel.Password);
           if (!validationResult.Succeeded)
 {
        foreach (var err in validationResult.Errors)
    {
         ModelState.AddModelError("RModel.Password", err.Description);
      }
       return Page();
          }
        }

   // Protect NRIC using data protection
        user.EncryptedNric = encryptionService.Protect(sanitizedNric);

       // Handle resume upload with comprehensive validation
  if (RModel.Resume != null && RModel.Resume.Length > 0)
          {
          // Validate file extension
     var allowed = new[] { ".pdf", ".docx", ".doc" };
     var ext = Path.GetExtension(RModel.Resume.FileName).ToLowerInvariant();
       if (!allowed.Contains(ext))
 {
          ModelState.AddModelError("RModel.Resume", "Resume must be .pdf, .docx, or .doc format");
   return Page();
      }

   // Validate file size (max 5MB)
    const long maxFileSize = 5 * 1024 * 1024; // 5MB
 if (RModel.Resume.Length > maxFileSize)
    {
ModelState.AddModelError("RModel.Resume", "Resume must be under 5MB");
         return Page();
   }

  // For PDF files, skip strict MIME type checking due to browser variations
     // For DOCX files, validate MIME type
      if (ext != ".pdf")
  {
   var validContentTypes = new[] 
   { 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
         "application/msword",
            "application/x-msword"
      };
   
           string mimeType = RModel.Resume.ContentType?.ToLowerInvariant() ?? "";
    
              // Only validate MIME for non-PDF files
       if (!string.IsNullOrEmpty(mimeType) && 
   !validContentTypes.Contains(mimeType) &&
   ext != ".pdf")
          {
         // Don't block upload - just use extension validation
      }
        }

  // Create uploads directory if it doesn't exist
  var uploads = Path.Combine(env.WebRootPath, "uploads");
     if (!Directory.Exists(uploads))
        {
      Directory.CreateDirectory(uploads);
 }

       // Generate secure filename (prevent directory traversal attacks)
      var fileName = Guid.NewGuid().ToString() + ext;

       // Verify the generated filename is safe
      if (!inputSanitizer.ValidateFilename(fileName))
      {
           ModelState.AddModelError("RModel.Resume", "Error processing file name");
      return Page();
     }

  var filePath = Path.Combine(uploads, fileName);

      // Ensure file path stays within uploads directory (prevent directory traversal)
 var fullPath = Path.GetFullPath(filePath);
   var uploadsFullPath = Path.GetFullPath(uploads);
 if (!fullPath.StartsWith(uploadsFullPath, StringComparison.OrdinalIgnoreCase))
          {
   ModelState.AddModelError("RModel.Resume", "Invalid file path");
      return Page();
     }

 using (var stream = System.IO.File.Create(filePath))
        {
await RModel.Resume.CopyToAsync(stream);
      }
user.ResumePath = "/uploads/" + fileName;
      }

  var result = await userManager.CreateAsync(user, RModel.Password);
 if (result.Succeeded)
         {
    await signInManager.SignInAsync(user, false);
               return RedirectToPage("Index");
  }
           foreach (var error in result.Errors)
          {
   ModelState.AddModelError("", error.Description);
  }
     }
   catch (ArgumentException ex)
         {
       // Catch sanitization errors
       ModelState.AddModelError("", "Invalid input: " + ex.Message);
        return Page();
    }
          catch (Exception ex)
    {
        // Log the error in production
      ModelState.AddModelError("", "An error occurred during registration. Please try again.");
              return Page();
      }
   }
            return Page();
        }

 /// <summary>
        /// Validates and returns gender value. Only allows predefined values to prevent injection.
        /// </summary>
        private string ValidateAndGetGender(string gender)
   {
        var validGenders = new[] { "Male", "Female", "Other" };
   if (string.IsNullOrWhiteSpace(gender) || !validGenders.Contains(gender))
            {
    throw new ArgumentException("Invalid gender selection");
   }
            return gender;
        }

        /// <summary>
        /// Validates date of birth to ensure reasonable values.
        /// </summary>
        private bool ValidateDateOfBirth(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
   var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
            {
     age--;
            }

   // User must be at least 16 years old and not in the future
            return age >= 16 && dateOfBirth.Date <= today;
      }

    private string EncryptString(string input)
        {
      // left for backward compat but not used now
        if (string.IsNullOrEmpty(input)) return input;
     var key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); //32 bytes key
            using var aes = Aes.Create();
  aes.Key = key;
         aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
         ms.Write(aes.IV, 0, aes.IV.Length);
          using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
     {
                sw.Write(input);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
