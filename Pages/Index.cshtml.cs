using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    [Authorize] // Ensure only authenticated users can access the home/index page
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;

        public List<UserDto> Users { get; set; } = new();

        // Properties for the currently signed-in user
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string DecryptedNric { get; set; }

        public IndexModel(ILogger<IndexModel> logger, UserManager<ApplicationUser> userManager, IEncryptionService encryptionService)
        {
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public async Task OnGet()
        {
            // Load all users from the user store
            var allUsers = _userManager.Users.ToList();
            foreach (var u in allUsers)
            {
                var dto = new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    WhoAmI = u.WhoAmI,
                    ResumePath = u.ResumePath,
                    DecryptedNric = _encryptionService.Unprotect(u.EncryptedNric)
                };
                Users.Add(dto);
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                var current = await _userManager.GetUserAsync(User);
                if (current != null)
                {
                    FullName = current.FirstName + " " + current.LastName;
                    Email = current.Email;
                    Gender = current.Gender;
                    DateOfBirth = current.DateOfBirth;
                    DecryptedNric = _encryptionService.Unprotect(current.EncryptedNric);
                }
            }
        }

        public class UserDto
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Gender { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string WhoAmI { get; set; }
            public string ResumePath { get; set; }
            public string DecryptedNric { get; set; }
        }
    }
}
