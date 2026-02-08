using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        // Store protected/encrypted NRIC
        public string EncryptedNric { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string WhoAmI { get; set; }
        // Path to uploaded resume
        public string ResumePath { get; set; }

        // Current active session identifier (used to detect concurrent logins)
        public string? CurrentSessionId { get; set; }

        // Track the last password change timestamp for password age enforcement
        public DateTime? LastPasswordChangedAt { get; set; }
    }
}
