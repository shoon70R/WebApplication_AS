using Microsoft.AspNetCore.DataProtection;

namespace WebApplication1.Model
{
 public interface IEncryptionService
 {
 string Protect(string plaintext);
 string Unprotect(string protectedData);
 }

 public class EncryptionService : IEncryptionService
 {
 private readonly IDataProtector _protector;

 public EncryptionService(IDataProtector protector)
 {
 _protector = protector;
 }

 public string Protect(string plaintext)
 {
 if (string.IsNullOrEmpty(plaintext)) return plaintext;
 return _protector.Protect(plaintext);
 }

 public string Unprotect(string protectedData)
 {
 if (string.IsNullOrEmpty(protectedData)) return protectedData;
 try
 {
 return _protector.Unprotect(protectedData);
 }
 catch
 {
 return null;
 }
 }
 }
}
