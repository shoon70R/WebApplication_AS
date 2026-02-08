using System;

namespace WebApplication1.Model
{
 public class AuditLog
 {
 public int Id { get; set; }
 public string UserId { get; set; }
 public string Action { get; set; }
 public DateTime Timestamp { get; set; }
 public string IpAddress { get; set; }
 }
}
