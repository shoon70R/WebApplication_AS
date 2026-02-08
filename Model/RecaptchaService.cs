using System.Text.Json.Serialization;

namespace WebApplication1.Model
{
    public interface IRecaptchaService
    {
        Task<RecaptchaVerificationResult> VerifyAsync(string token);
    }

    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private const string VerificationUrl = "https://www.google.com/recaptcha/api/siteverify";
        private const double ScoreThreshold = 0.5; // Threshold for bot detection (0.0 - 1.0)

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration)
     {
         _httpClient = httpClient;
       _secretKey = configuration["Recaptcha:SecretKey"] ?? throw new InvalidOperationException("Recaptcha:SecretKey is not configured.");
     }

        public async Task<RecaptchaVerificationResult> VerifyAsync(string token)
        {
   if (string.IsNullOrWhiteSpace(token))
            {
  return new RecaptchaVerificationResult
                {
        Success = false,
          ErrorMessage = "reCAPTCHA token is missing"
     };
      }

         try
          {
                var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("secret", _secretKey),
     new KeyValuePair<string, string>("response", token)
      });

     var response = await _httpClient.PostAsync(VerificationUrl, content);
     response.EnsureSuccessStatusCode();

       var jsonResponse = await response.Content.ReadAsStringAsync();
     var result = System.Text.Json.JsonSerializer.Deserialize<RecaptchaApiResponse>(jsonResponse, 
       new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

     if (result == null)
          {
    return new RecaptchaVerificationResult
           {
         Success = false,
        ErrorMessage = "Failed to parse reCAPTCHA response"
           };
        }

        // Check if verification was successful
      if (!result.Success)
           {
       var errors = result.ErrorCodes?.Any() == true ? string.Join(", ", result.ErrorCodes) : "Unknown error";
     return new RecaptchaVerificationResult
       {
          Success = false,
   ErrorMessage = $"reCAPTCHA verification failed: {errors}"
          };
     }

    // Check score threshold to detect bots (lower score = more likely a bot)
     if (result.Score < ScoreThreshold)
      {
           return new RecaptchaVerificationResult
{
        Success = false,
          ErrorMessage = "Suspicious activity detected. Please try again.",
         Score = result.Score,
         IsSuspiciousActivity = true
        };
                }

                return new RecaptchaVerificationResult
    {
         Success = true,
Score = result.Score,
           Action = result.Action,
   ChallengeTimestamp = result.ChallengeTimestamp,
        Hostname = result.Hostname
    };
   }
            catch (HttpRequestException ex)
        {
             return new RecaptchaVerificationResult
           {
     Success = false,
        ErrorMessage = $"Failed to verify reCAPTCHA: {ex.Message}"
};
            }
       catch (Exception ex)
            {
return new RecaptchaVerificationResult
      {
     Success = false,
         ErrorMessage = $"An error occurred during reCAPTCHA verification: {ex.Message}"
          };
            }
     }
    }

    public class RecaptchaVerificationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double Score { get; set; }
    public string? Action { get; set; }
        public DateTime? ChallengeTimestamp { get; set; }
     public string? Hostname { get; set; }
        public bool IsSuspiciousActivity { get; set; }
    }

    public class RecaptchaApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
      public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
     public string[]? ErrorCodes { get; set; }
    }
}
