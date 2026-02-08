using Microsoft.AspNetCore.Identity;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public interface IPasswordHistoryService
    {
     /// <summary>
        /// Checks if the new password matches any of the user's recent password history
/// </summary>
    Task<bool> IsPasswordReusedAsync(ApplicationUser user, string newPassword);

        /// <summary>
  /// Adds the current password to the user's password history
        /// </summary>
        Task AddPasswordToHistoryAsync(string userId, string passwordHash);

 /// <summary>
        /// Cleans up old password history entries, keeping only the last N passwords
     /// </summary>
 Task CleanupOldPasswordsAsync(string userId, int maxHistoryCount = 2);

     /// <summary>
      /// Checks if the user can change their password (enforces minimum password age)
      /// Returns tuple: (canChange, minutesUntilCanChange)
     /// </summary>
    Task<(bool canChange, int minutesUntilCanChange)> CanChangePasswordAsync(ApplicationUser user);

    /// <summary>
        /// Checks if the user must change their password (enforces maximum password age)
/// </summary>
      Task<bool> MustChangePasswordAsync(ApplicationUser user);

       /// <summary>
        /// Updates the LastPasswordChangedAt timestamp for a user
        /// </summary>
      Task UpdateLastPasswordChangedAsync(string userId);
    }

    public class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly AuthDbContext _db;
     private readonly UserManager<ApplicationUser> _userManager;
     private readonly IPasswordPolicyService _policyService;

        public PasswordHistoryService(AuthDbContext db, UserManager<ApplicationUser> userManager, IPasswordPolicyService policyService)
      {
     _db = db;
      _userManager = userManager;
  _policyService = policyService;
}

        /// <summary>
     /// Checks if the new password matches any of the user's recent password history
    /// </summary>
        public async Task<bool> IsPasswordReusedAsync(ApplicationUser user, string newPassword)
  {
     var maxPasswordHistory = _policyService.GetPasswordHistoryCount();

        // Get the user's recent password history
var passwordHistory = _db.PasswordHistories
 .Where(ph => ph.UserId == user.Id)
    .OrderByDescending(ph => ph.CreatedAt)
.Take(maxPasswordHistory)
.ToList();

      // Check if new password matches any historical password
     foreach (var history in passwordHistory)
     {
        // Use the password hasher to verify the new password against historical hashes
     var result = _userManager.PasswordHasher.VerifyHashedPassword(user, history.PasswordHash, newPassword);
       if (result != PasswordVerificationResult.Failed)
 {
     return true; // Password has been used before
 }
}

       return false; // Password is new
   }

    /// <summary>
      /// Adds the current password to the user's password history
        /// </summary>
        public async Task AddPasswordToHistoryAsync(string userId, string passwordHash)
        {
    var passwordHistory = new PasswordHistory
    {
     UserId = userId,
     PasswordHash = passwordHash,
  CreatedAt = DateTime.UtcNow
    };

      _db.PasswordHistories.Add(passwordHistory);
   await _db.SaveChangesAsync();

      var maxPasswordHistory = _policyService.GetPasswordHistoryCount();
    // Clean up old passwords, keeping only the last N entries
      await CleanupOldPasswordsAsync(userId, maxPasswordHistory);
  }

     /// <summary>
  /// Cleans up old password history entries, keeping only the last N passwords
 /// </summary>
  public async Task CleanupOldPasswordsAsync(string userId, int maxHistoryCount = 2)
        {
     // Get all password history for the user, ordered by most recent
     var allPasswords = _db.PasswordHistories
.Where(ph => ph.UserId == userId)
 .OrderByDescending(ph => ph.CreatedAt)
.ToList();

  // If we have more than maxHistoryCount, delete the oldest ones
    if (allPasswords.Count > maxHistoryCount)
       {
   var passwordsToDelete = allPasswords.Skip(maxHistoryCount).ToList();
        _db.PasswordHistories.RemoveRange(passwordsToDelete);
   await _db.SaveChangesAsync();
  }
   }

   /// <summary>
        /// Checks if the user can change their password (enforces minimum password age)
        /// Returns tuple: (canChange, minutesUntilCanChange)
      /// </summary>
   public async Task<(bool canChange, int minutesUntilCanChange)> CanChangePasswordAsync(ApplicationUser user)
     {
      var minPasswordAgeMinutes = _policyService.GetMinPasswordAgeMinutes();

      // If user has never changed password, they can change it
       if (user.LastPasswordChangedAt == null)
   {
   return (true, 0);
      }

    var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangedAt.Value;
         var minutesElapsed = (int)timeSinceLastChange.TotalMinutes;
    var minutesUntilCanChange = minPasswordAgeMinutes - minutesElapsed;

    if (minutesUntilCanChange <= 0)
    {
         return (true, 0);
     }

    return (false, minutesUntilCanChange);
    }

 /// <summary>
   /// Checks if the user must change their password (enforces maximum password age)
        /// </summary>
       public async Task<bool> MustChangePasswordAsync(ApplicationUser user)
        {
    var maxPasswordAgeMinutes = _policyService.GetMaxPasswordAgeMinutes();
    var maxPasswordAgeDays = _policyService.GetMaxPasswordAgeDays();

    // If user has never changed password, they don't need to change yet
   if (user.LastPasswordChangedAt == null)
       {
       return false;
     }

   var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangedAt.Value;
   
   // Check minutes-based maximum age first (if configured, takes precedence)
   if (maxPasswordAgeMinutes > 0)
   {
      var minutesElapsed = (int)timeSinceLastChange.TotalMinutes;
      return minutesElapsed > maxPasswordAgeMinutes;
   }
   
   // Otherwise check days-based maximum age
   var daysElapsed = timeSinceLastChange.TotalDays;
   return daysElapsed > maxPasswordAgeDays;
       }

  /// <summary>
/// Updates the LastPasswordChangedAt timestamp for a user
        /// </summary>
      public async Task UpdateLastPasswordChangedAsync(string userId)
   {
          var user = await _userManager.FindByIdAsync(userId);
   if (user != null)
 {
      user.LastPasswordChangedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);
  }
   }
    }
}
