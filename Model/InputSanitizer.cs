using System.Text.RegularExpressions;

namespace WebApplication1.Model
{
    /// <summary>
    /// Service for sanitizing user inputs to prevent XSS and injection attacks.
    /// </summary>
    public interface IInputSanitizer
    {
        /// <summary>
        /// Sanitizes text input by removing potentially dangerous characters and SQL injection patterns.
        /// </summary>
      string SanitizeText(string input, int maxLength = 500);

        /// <summary>
        /// Validates and sanitizes email input.
        /// </summary>
        string SanitizeEmail(string input);

    /// <summary>
      /// Sanitizes multiline text input (like comments or descriptions).
      /// </summary>
        string SanitizeMultilineText(string input, int maxLength = 5000);

        /// <summary>
     /// Validates filename for file uploads to prevent directory traversal and injection.
        /// </summary>
        bool ValidateFilename(string filename);
    }

    public class InputSanitizer : IInputSanitizer
    {
   // Patterns that indicate potential SQL injection attempts
        private static readonly string[] SqlInjectionPatterns = new[]
    {
 @"(\b(UNION|SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|SCRIPT|JAVASCRIPT|ONCLICK|ONERROR)\b)",
            @"(--|;|\/\*|\*\/|xp_|sp_)",
            @"(['""<>])",
    @"(\b(OR|AND)\b\s*1\s*=\s*1)",
     @"(;.*DROP|;.*DELETE|;.*UPDATE|;.*INSERT)"
        };

   // XSS patterns
        private static readonly string[] XssPatterns = new[]
        {
       @"<script[^>]*>.*?</script>",
         @"javascript:",
            @"on\w+\s*=",
            @"<iframe",
       @"<object",
            @"<embed",
      @"<img[^>]*on\w+",
            @"<body[^>]*on\w+",
   @"<svg[^>]*on\w+"
        };

  /// <summary>
        /// Sanitizes text input by removing potentially dangerous characters and patterns.
        /// </summary>
        public string SanitizeText(string input, int maxLength = 500)
        {
       if (string.IsNullOrWhiteSpace(input))
   return string.Empty;

  // Trim and enforce max length
            input = input.Trim().Substring(0, Math.Min(input.Length, maxLength));

        // Check for SQL injection patterns
if (ContainsSqlInjectionPatterns(input))
           throw new ArgumentException("Input contains invalid characters or patterns.");

            // Check for XSS patterns
          if (ContainsXssPatterns(input))
         throw new ArgumentException("Input contains invalid HTML or script patterns.");

   // Remove control characters except newline and tab (for multiline text)
         input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

 // HTML encode dangerous characters
     input = HtmlEncode(input);

      return input;
        }

        /// <summary>
        /// Sanitizes email input with validation.
    /// </summary>
        public string SanitizeEmail(string input)
      {
      if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

        input = input.Trim().ToLowerInvariant();

   // Enforce max length for email
  if (input.Length > 254)
throw new ArgumentException("Email address is too long.");

          // Check for SQL injection patterns
            if (ContainsSqlInjectionPatterns(input))
              throw new ArgumentException("Email contains invalid characters.");

  // Validate email format using regex
            if (!Regex.IsMatch(input, @"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$"))
      throw new ArgumentException("Email format is invalid.");

  return input;
        }

     /// <summary>
    /// Sanitizes multiline text input like comments or descriptions.
        /// </summary>
        public string SanitizeMultilineText(string input, int maxLength = 5000)
        {
         if (string.IsNullOrWhiteSpace(input))
          return string.Empty;

            // Trim and enforce max length
       input = input.Trim().Substring(0, Math.Min(input.Length, maxLength));

      // Check for SQL injection patterns
         if (ContainsSqlInjectionPatterns(input))
      throw new ArgumentException("Input contains invalid characters or patterns.");

      // Check for XSS patterns
       if (ContainsXssPatterns(input))
      throw new ArgumentException("Input contains invalid HTML or script patterns.");

            // Remove control characters except newline and tab
   input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

          // HTML encode dangerous characters while preserving line breaks
  input = HtmlEncode(input);

            return input;
        }

        /// <summary>
 /// Validates filename to prevent directory traversal and path injection attacks.
        /// </summary>
public bool ValidateFilename(string filename)
        {
   if (string.IsNullOrWhiteSpace(filename))
                return false;

            // Check for directory traversal attempts
      if (filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
            return false;

      // Allow only alphanumeric, dash, underscore, and dot
          if (!Regex.IsMatch(filename, @"^[a-zA-Z0-9._-]+$"))
    return false;

   return true;
        }

    /// <summary>
    /// Checks if input contains SQL injection patterns.
        /// </summary>
        private bool ContainsSqlInjectionPatterns(string input)
        {
            foreach (var pattern in SqlInjectionPatterns)
   {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
     return true;
      }
return false;
   }

        /// <summary>
/// Checks if input contains XSS patterns.
        /// </summary>
        private bool ContainsXssPatterns(string input)
     {
   foreach (var pattern in XssPatterns)
     {
        if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
          return true;
            }
return false;
     }

        /// <summary>
/// HTML encodes special characters to prevent XSS.
    /// </summary>
      private string HtmlEncode(string input)
        {
            return System.Net.WebUtility.HtmlEncode(input);
        }
    }
}
