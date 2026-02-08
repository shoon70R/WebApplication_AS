using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace WebApplication1.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sends an email message using Google OAuth2 via SMTP
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // Get Google OAuth2 settings from configuration
                var clientId = _configuration["Google:ClientId"];
                var clientSecret = _configuration["Google:ClientSecret"];
                var refreshToken = _configuration["Google:RefreshToken"];
                var fromEmail = _configuration["Smtp:FromEmail"];
                var fromName = _configuration["Smtp:FromName"] ?? "Ace Job Agency";

                // Validate configuration
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) ||
                    string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("Google OAuth2 configuration is incomplete. Required: Google:ClientId, Google:ClientSecret, Google:RefreshToken, Smtp:FromEmail");
                    return false;
                }

                // Get access token using refresh token
                var accessToken = await GetGoogleAccessTokenAsync(clientId, clientSecret, refreshToken);

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to obtain Google access token");
                    return false;
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                // Send via SMTP using OAuth2
                using (var client = new SmtpClient())
                {
                    // Connect to Gmail SMTP server
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                    // Authenticate using OAuth2
                    var oauth2 = new SaslMechanismOAuth2(fromEmail, accessToken);
                    await client.AuthenticateAsync(oauth2);

                    // Send email
                    await client.SendAsync(message);

                    // Disconnect
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a new access token using the refresh token
        /// </summary>
        private async Task<string> GetGoogleAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
        {
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = clientId,
                            ClientSecret = clientSecret
                        }
                    });

                var token = new TokenResponse
                {
                    RefreshToken = refreshToken
                };

                // Refresh the token to get a new access token
                var result = await flow.RefreshTokenAsync("user", refreshToken, System.Threading.CancellationToken.None);

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obtaining Google access token: {ex.Message}");
                return null;
            }
        }
    }
}
