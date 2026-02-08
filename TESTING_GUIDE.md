# Password Reuse Prevention Testing Guide

## Overview
This guide will help you test the password reuse prevention feature (max 2 password history) in your Razor Pages application.

---

## **Test Scenario 1: Verify Password Change Works (Baseline)**

### Steps:
1. **Start the application**
   - Run the application in Visual Studio or via `dotnet run`
   - Navigate to `https://localhost:7001` (or your configured port)

2. **Login with test user**
   - Email: `testuser@example.com`
   - Password: `TestPass123!` (or any existing password)
   - If you don't have a test user, create one via the Register page first

3. **Navigate to Change Password**
   - After login, you'll see the "Welcome Back!" page
   - Click the **"Change Password"** button

4. **Change to Password #1**
   - Current Password: `TestPass123!`
   - New Password: `NewPass1234!`
   - Confirm Password: `NewPass1234!`
   - Click **"Change Password"**
   - ? **Expected Result**: Success message appears, then redirects to login after 3 seconds

5. **Verify Login with New Password**
   - Login with: `NewPass1234!`
   - ? **Expected Result**: Login succeeds

---

## **Test Scenario 2: Try to Reuse Password #1 (Should Fail)**

### Steps:
1. **Login again**
   - Email: `testuser@example.com`
   - Password: `NewPass1234!`

2. **Go to Change Password**
   - Click **"Change Password"** button

3. **Try to Change Back to Password #1**
   - Current Password: `NewPass1234!`
   - New Password: `TestPass123!` ? **This was the ORIGINAL password**
   - Confirm Password: `TestPass123!`
   - Click **"Change Password"**
   - ? **Expected Result**: Error message appears:
     ```
"You cannot reuse one of your last 2 passwords. Please choose a different password."
     ```

4. **Verify No Change Occurred**
   - Try to login with `TestPass123!`
   - ? **Expected Result**: Login fails (password was NOT changed)

---

## **Test Scenario 3: Change to Password #2 (Should Succeed)**

### Steps:
1. **Login with Current Password**
   - Email: `testuser@example.com`
   - Password: `NewPass1234!`

2. **Go to Change Password**

3. **Change to a Brand New Password #2**
   - Current Password: `NewPass1234!`
   - New Password: `AnotherPass567!` ? **Completely new password**
   - Confirm Password: `AnotherPass567!`
   - Click **"Change Password"**
   - ? **Expected Result**: Success message appears

4. **Verify Login with Password #2**
   - Login with: `AnotherPass567!`
   - ? **Expected Result**: Login succeeds

---

## **Test Scenario 4: Try to Reuse Password #2 (Should Fail)**

### Steps:
1. **Login with Current Password**
   - Email: `testuser@example.com`
   - Password: `AnotherPass567!`

2. **Go to Change Password**

3. **Try to Reuse Password #2**
   - Current Password: `AnotherPass567!`
   - New Password: `NewPass1234!` ? **This was the SECOND most recent password**
   - Confirm Password: `NewPass1234!`
   - Click **"Change Password"**
   - ? **Expected Result**: Error message:
     ```
     "You cannot reuse one of your last 2 passwords. Please choose a different password."
     ```

---

## **Test Scenario 5: Change to Password #3 (Oldest Password Now Allowed)**

### Steps:
1. **Login with Current Password**
   - Email: `testuser@example.com`
   - Password: `AnotherPass567!`

2. **Go to Change Password**

3. **Change to Password #3 (Oldest is now allowed)**
   - Current Password: `AnotherPass567!`
   - New Password: `TestPass123!` ? **This is now OK (oldest password)**
- Confirm Password: `TestPass123!`
   - Click **"Change Password"**
   - ? **Expected Result**: Success message appears
   - ?? **Why?**: Only last 2 passwords are blocked. After 3 changes:
     - Password #1: `TestPass123!` (oldest - now allowed again)
     - Password #2: `NewPass1234!` (blocked)
     - Password #3: `AnotherPass567!` (blocked)

4. **Verify Login**
   - Login with: `TestPass123!`
   - ? **Expected Result**: Login succeeds

---

## **Test Scenario 6: Verify Same Password Error**

### Steps:
1. **Login**
   - Email: `testuser@example.com`
   - Current Password: `TestPass123!`

2. **Go to Change Password**

3. **Try to Use Same Password as Current**
   - Current Password: `TestPass123!`
   - New Password: `TestPass123!` ? **Same as current**
   - Confirm Password: `TestPass123!`
   - Click **"Change Password"**
   - ? **Expected Result**: Error message:
     ```
     "New password must be different from the current password."
     ```

---

## **Test Scenario 7: Verify Incorrect Current Password Error**

### Steps:
1. **Login**
   - Email: `testuser@example.com`
   - Password: `TestPass123!`

2. **Go to Change Password**

3. **Enter Wrong Current Password**
   - Current Password: `WrongPassword123!` ? **Wrong!**
   - New Password: `CompletelyNew789!`
   - Confirm Password: `CompletelyNew789!`
 - Click **"Change Password"**
   - ? **Expected Result**: Error message:
  ```
     "Current password is incorrect."
     ```

---

## **Database Verification (Optional)**

### Check Password History in Database:

1. **Open SQL Server Management Studio (SSMS)**
   - Connect to your database server

2. **Run this query** to see password history:
```sql
SELECT 
    ph.Id,
  ph.UserId,
    ph.PasswordHash,
    ph.CreatedAt
FROM PasswordHistories ph
ORDER BY ph.UserId, ph.CreatedAt DESC;
```

3. **Expected Result**:
   - For your test user, you should see multiple entries
   - Each entry shows a password hash and when it was set
   - The most recent 2 entries are blocked from reuse

### Check Audit Logs:

```sql
SELECT 
    al.Id,
    al.UserId,
    al.Action,
    al.Timestamp,
    al.IpAddress
FROM AuditLogs al
WHERE al.Action LIKE '%ChangePassword%'
ORDER BY al.Timestamp DESC;
```

4. **Expected Actions**:
   - `ChangePasswordSuccess` - Successful password changes
   - `ChangePasswordFailed-IncorrectPassword` - Wrong current password
   - `ChangePasswordFailed-PasswordReused` - Attempted password reuse
   - `ChangePasswordFailed-ValidationError` - Password doesn't meet requirements

---

## **UI Testing Checklist**

- [ ] Password visibility toggle (eye icon) works on all 3 fields
- [ ] Eye icon shows/hides password correctly
- [ ] Form validation messages display properly
- [ ] Success alert appears for 3 seconds before redirecting
- [ ] Redirect to login page works after success
- [ ] Error messages are clear and helpful
- [ ] Cancel button returns to profile page
- [ ] Page is responsive on mobile/tablet

---

## **Security Testing Checklist**

- [ ] Current password is verified before any change
- [ ] New password meets complexity requirements (12+ chars, upper, lower, digit, special)
- [ ] Password confirmation matches validation
- [ ] Old passwords in history are checked correctly
- [ ] User is redirected to login after successful change
- [ ] Session is properly invalidated after password change
- [ ] Audit logs record all password change attempts
- [ ] Error messages don't reveal sensitive information

---

## **Summary of Test Results**

| Test Scenario | Expected Outcome | Status |
|---|---|---|
| Change password to new password | Success + Redirect | ? Pass / ? Fail |
| Reuse 1st most recent password | Error + No change | ? Pass / ? Fail |
| Reuse 2nd most recent password | Error + No change | ? Pass / ? Fail |
| Reuse oldest password (after 3 changes) | Success | ? Pass / ? Fail |
| Use same password as current | Error + No change | ? Pass / ? Fail |
| Wrong current password | Error + No change | ? Pass / ? Fail |
| Password doesn't meet requirements | Error + No change | ? Pass / ? Fail |

---

## **Common Test Passwords**

Use these for testing (they meet the 12-char requirement):
- `TestPass123!`
- `NewPass1234!`
- `AnotherPass567!`
- `CompletelyNew789!`
- `SecurePass2024!`
- `MyPassword999@`

---

## **Troubleshooting**

### Issue: "PasswordHistories table not found"
**Solution**: Run migrations:
```bash
dotnet ef database update
```

### Issue: Password change succeeds but reuse check doesn't work
**Solution**: Verify `IPasswordHistoryService` is registered in `Program.cs`

### Issue: Audit logs not showing
**Solution**: Check `AuditLogs` table exists in database

### Issue: Redirect not working after success
**Solution**: Check browser console for JavaScript errors (F12 Developer Tools)

---

