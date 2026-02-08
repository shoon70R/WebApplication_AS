using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace WebApplication1.Pages
{
    [IgnoreAntiforgeryToken]
    public class StatusCodeModel : PageModel
    {
        private readonly ILogger<StatusCodeModel> _logger;

  public int StatusCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public StatusCodeModel(ILogger<StatusCodeModel> logger)
     {
            _logger = logger;
   }

   public void OnGet(int statusCode)
        {
     StatusCode = statusCode;
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

       // Map status codes to user-friendly messages
            (ErrorMessage, ErrorDescription) = statusCode switch
          {
                400 => ("Bad Request", "The request you made was invalid or malformed. Please check your input and try again."),
 401 => ("Unauthorized", "You are not authorized to access this resource. Please log in."),
      403 => ("Access Denied", "You do not have permission to access this resource."),
     404 => ("Page Not Found", "The page you are looking for could not be found. It may have been moved or deleted."),
405 => ("Method Not Allowed", "The HTTP method used is not supported for this resource."),
                408 => ("Request Timeout", "The request took too long to process. Please try again."),
           409 => ("Conflict", "The request conflicts with the current state of the server."),
         410 => ("Gone", "The requested resource is no longer available and will not be available again."),
          415 => ("Unsupported Media Type", "The request entity has a media type which the server does not support."),
         429 => ("Too Many Requests", "You have sent too many requests in a short period. Please wait and try again."),
     500 => ("Internal Server Error", "An unexpected error occurred on the server. Our team has been notified."),
       501 => ("Not Implemented", "This feature is not yet implemented."),
        502 => ("Bad Gateway", "The server received an invalid response from an upstream server."),
    503 => ("Service Unavailable", "The server is temporarily unavailable. Please try again later."),
         504 => ("Gateway Timeout", "The upstream server failed to respond in time."),
  _ => ($"Error {statusCode}", "An error occurred while processing your request.")
    };

        // Log the error
            _logger.LogWarning($"Status code {statusCode} - RequestId: {RequestId}");
        }
}
}
