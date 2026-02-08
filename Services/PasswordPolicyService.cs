using WebApplication1.Model;

namespace WebApplication1.Services
{
  /// <summary>
    /// Service for managing password policy settings
    /// </summary>
    public interface IPasswordPolicyService
    {
   /// <summary>
        /// Gets the minimum password age in minutes
  /// </summary>
        int GetMinPasswordAgeMinutes();

        /// <summary>
        /// Gets the maximum password age in days
        /// </summary>
  int GetMaxPasswordAgeDays();

     /// <summary>
     /// Gets the maximum password age in minutes (used if set, takes precedence over days)
     /// </summary>
  int GetMaxPasswordAgeMinutes();

        /// <summary>
  /// Gets the number of previous passwords to check for reuse
      /// </summary>
        int GetPasswordHistoryCount();
    }

    public class PasswordPolicyService : IPasswordPolicyService
    {
        private readonly IConfiguration _configuration;

  public PasswordPolicyService(IConfiguration configuration)
{
            _configuration = configuration;
   }

 /// <summary>
/// Gets the minimum password age in minutes
        /// Default: 1 minute (prevents users from changing password too frequently)
  /// </summary>
        public int GetMinPasswordAgeMinutes()
        {
        var configValue = _configuration["PasswordPolicy:MinPasswordAgeMinutes"];
    if (int.TryParse(configValue, out var minutes) && minutes >= 0)
        {
     return minutes;
     }
            return 1; // Default: 1 minute
 }

        /// <summary>
   /// Gets the maximum password age in days
    /// Default: 90 days (users must change password every 90 days)
  /// </summary>
        public int GetMaxPasswordAgeDays()
        {
  var configValue = _configuration["PasswordPolicy:MaxPasswordAgeDays"];
         if (int.TryParse(configValue, out var days) && days > 0)
       {
     return days;
         }
          return 90; // Default: 90 days
        }

     /// <summary>
   /// Gets the maximum password age in minutes (used if set, takes precedence over days)
        /// Default: 0 (disabled, uses days instead)
     /// </summary>
    public int GetMaxPasswordAgeMinutes()
      {
      var configValue = _configuration["PasswordPolicy:MaxPasswordAgeMinutes"];
 if (int.TryParse(configValue, out var minutes) && minutes > 0)
      {
        return minutes;
    }
   return 0; // Default: 0 (use days instead)
        }

   /// <summary>
        /// Gets the number of previous passwords to check for reuse
        /// Default: 2 passwords (cannot reuse last 2 passwords)
 /// </summary>
        public int GetPasswordHistoryCount()
        {
            var configValue = _configuration["PasswordPolicy:PasswordHistoryCount"];
    if (int.TryParse(configValue, out var count) && count > 0)
 {
      return count;
   }
  return 2; // Default: 2 passwords
        }
 }
}
