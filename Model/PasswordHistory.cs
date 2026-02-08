using System;

namespace WebApplication1.Model
{
    public class PasswordHistory
    {
        public int Id { get; set; }
 public string UserId { get; set; }
        // Store the hashed password
        public string PasswordHash { get; set; }
        // When this password was set
    public DateTime CreatedAt { get; set; }
    }
}
