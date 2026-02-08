using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    [Authorize]
    public class ForbiddenTestModel : PageModel
    {
        private readonly ILogger<ForbiddenTestModel> _logger;

        public ForbiddenTestModel(ILogger<ForbiddenTestModel> logger)
        {
            _logger = logger;
     }

      public IActionResult OnGet()
        {
_logger.LogInformation($"User {User.Identity?.Name} attempted to access forbidden resource");
     // Redirect to StatusCode page with 403 parameter to display access denied page
    return RedirectToPage("/StatusCode", new { statusCode = 403 });
   }
    }
}
