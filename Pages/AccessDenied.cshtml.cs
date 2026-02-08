using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace WebApplication1.Pages
{
    public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        public string? RequestId { get; set; }
   public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
        {
 _logger = logger;
}

   public void OnGet()
        {
  RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogWarning($"Access Denied (403) - User: {User?.Identity?.Name ?? "Anonymous"} - RequestId: {RequestId}");
      }
    }
}
