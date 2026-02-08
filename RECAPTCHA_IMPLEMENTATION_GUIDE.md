# Google reCAPTCHA v3 Implementation Guide

## Overview
Google reCAPTCHA v3 is an invisible antibot service that detects suspicious user activity. Unlike reCAPTCHA v2, it doesn't require users to solve puzzles but runs in the background and returns a score (0.0 to 1.0) indicating the likelihood of being a bot.

## Implementation Summary

### Files Modified/Created:
1. **Model/RecaptchaService.cs** (NEW) - reCAPTCHA verification service
2. **Program.cs** - Service registration
3. **Pages/Login.cshtml.cs** - Token verification during login
4. **Pages/Login.cshtml** - reCAPTCHA script integration
5. **appsettings.json** - Configuration

---

## Setup Instructions

### Step 1: Obtain reCAPTCHA Keys from Google

1. Go to [Google reCAPTCHA Console](https://www.google.com/recaptcha/admin)
2. Sign in with your Google account
3. Click **Create** or **+** button to create a new site
4. Fill in the form:
   - **Label**: Your Application Name
   - **reCAPTCHA type**: Select "reCAPTCHA v3"
   - **Domains**: Add your domain(s):
     - For development: `localhost`
     - For production: `yourdomain.com`
5. Accept the terms and click **Submit**
6. Copy your:
   - **Site Key** (Client-side)
   - **Secret Key** (Server-side)

### Step 2: Update Configuration

Replace the placeholder keys in `appsettings.json`:

```json
{
  "Recaptcha": {
    "SiteKey": "YOUR_ACTUAL_SITE_KEY",
    "SecretKey": "YOUR_ACTUAL_SECRET_KEY"
  }
}
```

**IMPORTANT**: 
- Never commit your Secret Key to public repositories
- Use user secrets for development: `dotnet user-secrets set "Recaptcha:SecretKey" "your-secret-key"`
- Use environment variables or Azure Key Vault in production

---

## How It Works

### 1. **Client-Side (JavaScript)**
   - When user submits login form, `grecaptcha.execute()` is called
   - This sends a request to Google's servers
   - Google returns a reCAPTCHA token (invisible to user)
   - Token is added to form and submitted with credentials

### 2. **Server-Side (C#)**
   - `RecaptchaService` receives the token
   - Sends token + Secret Key to Google's verification API
   - Google returns:
     - `success`: Boolean (verification passed)
     - `score`: 0.0 to 1.0 (0 = likely bot, 1.0 = likely human)
   - `action`: The action name (in this case: "login")
   - If score < 0.5 (default threshold), login is rejected
   - Failed attempts are logged in AuditLog

### 3. **Integration with Login Flow**
   ```
   User Submits Form
     ?
   Client-side: Request reCAPTCHA token
     ?
   Google: Return token + score
     ?
   Client: Add token to form, submit
     ?
   Server: Verify token with Google
     ?
   Check Score ? 0.5?
     ?? Yes ? Continue with email/password validation
     ?? No ? Reject with "Suspicious activity detected"
   ```

---

## Customization

### Adjust Bot Detection Threshold

Edit `Model/RecaptchaService.cs`:
```csharp
private const double ScoreThreshold = 0.5; // Change this value
```
- **Lower values** (e.g., 0.3): More lenient, fewer false positives
- **Higher values** (e.g., 0.7): More strict, better bot detection

### Add reCAPTCHA to Other Forms

Apply the same pattern to Register or other forms:

1. Add reCAPTCHA script to the page
2. Add hidden input for token:
   ```html
   <input type="hidden" id="recaptchaToken" name="RecaptchaToken" />
   ```
3. Attach event listener to form
4. Inject `IRecaptchaService` in PageModel
5. Call `_recaptchaService.VerifyAsync(token)` before processing

---

## Monitoring & Analytics

### Google reCAPTCHA Console Features:
- **Real-time Analytics**: View bot vs. human traffic
- **Score Distribution**: Monitor score trends
- **Top Invalid Requests**: See potential attacks

### Audit Logging
All failed login attempts are logged with reasons:
- `LoginFailed-MissingRecaptcha` - No token provided
- `LoginFailed-RecaptchaFailed` - Token verification failed
- `LoginFailed-UnknownUser` - Invalid email
- `LoginFailed` - Invalid password

---

## Security Best Practices

1. **Always verify on server**: Never trust client-side verification alone
2. **Use HTTPS**: reCAPTCHA requires secure connections
3. **Keep keys secure**: Never expose Secret Key in client code
4. **Set appropriate threshold**: Balance UX vs. security
5. **Monitor audit logs**: Watch for unusual login patterns
6. **Combine with rate limiting**: Add IP-based or email-based rate limiting

---

## Testing

### Local Testing:
1. Update `appsettings.json` with test keys (localhost domain)
2. Run application: `dotnet run`
3. Navigate to `/Login`
4. Submit form and observe:
   - Network tab: reCAPTCHA request to Google
   - Browser console: Token logged (if errors)
   - Form behavior: Should submit with token

### High-Score Testing:
- Normal browser behavior typically returns scores > 0.7
- Bot-like behavior (automation, headless browsers) returns low scores

### Low-Score Testing:
- Use headless browser or automation tools
- Should see "Suspicious activity detected" error

---

## Troubleshooting

### Issue: reCAPTCHA script not loading
- Check Site Key is correct
- Verify domain is added to Google reCAPTCHA console
- Use HTTPS for production

### Issue: Token verification always fails
- Confirm Secret Key is correct
- Check network connectivity to Google API
- Verify token is being sent with form

### Issue: Site Key showing in HTML
- This is **normal and expected** - Site Keys are public
- Secret Key should **never** appear in HTML

### Issue: High false positives
- Decrease score threshold (0.5 ? 0.3)
- Review Google Analytics in console

---

## Advanced: Manual Score Handling

Instead of automatic rejection, you can log the score and handle it differently:

```csharp
var recaptchaResult = await _recaptchaService.VerifyAsync(RecaptchaToken);

if (recaptchaResult.Score < 0.3)
{
    // Definitely suspicious - reject immediately
    ModelState.AddModelError("", "Suspicious activity detected");
    return Page();
}
else if (recaptchaResult.Score < 0.7)
{
    // Moderate risk - might require additional verification
    // Log for review, allow but flag account
}
else
{
    // Low risk - proceed normally
}
```

---

## Production Deployment

1. **Get production domain reCAPTCHA keys** from Google Console
2. **Use Key Management**:
   ```bash
   # Development
   dotnet user-secrets set "Recaptcha:SiteKey" "dev-site-key"
   dotnet user-secrets set "Recaptcha:SecretKey" "dev-secret-key"
   
   # Production - Use Azure Key Vault or environment variables
   ```
3. **Enable HTTPS**: reCAPTCHA requires secure connections
4. **Monitor**: Check Google Analytics regularly
5. **Test**: Verify functionality on staging environment

---

## Documentation Links

- [Google reCAPTCHA Documentation](https://developers.google.com/recaptcha/docs)
- [reCAPTCHA v3 Guide](https://developers.google.com/recaptcha/docs/v3)
- [Score Interpretation](https://developers.google.com/recaptcha/docs/v3#score_interpretation)

