using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    [Authorize]
    public class ProtectedPageModel : PageModel
    {
        private readonly ILogger<ProtectedPageModel> _logger;

    public ProtectedPageModel(ILogger<ProtectedPageModel> logger)
        {
         _logger = logger;
        }

      public void OnGet()
        {
     _logger.LogInformation($"Protected page accessed by user: {User.Identity?.Name}");
      }
    }
}
