using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace WebApplication1.Pages
{
    public class NotFoundModel : PageModel
    {
        private readonly ILogger<NotFoundModel> _logger;

   public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public NotFoundModel(ILogger<NotFoundModel> logger)
      {
            _logger = logger;
        }

     public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
      _logger.LogWarning($"Page Not Found (404) - Path: {Request.Path} - RequestId: {RequestId}");
        }
  }
}
