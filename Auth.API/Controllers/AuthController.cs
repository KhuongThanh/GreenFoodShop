using Auth.API.Contracts;
using Auth.API.Data;
using Auth.API.Entities;
using Auth.API.Security.Auth.API.Security;
using Auth.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Auth.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _db;

        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthController(AuthDbContext db, IEmailService emailService, ITokenService tokenService, IConfiguration config)
        {
            _db = db;
            _emailService = emailService;
            _tokenService = tokenService;
            _config = config;
        }

        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFa(EnableTwoFaRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("User not found");

            var twofa = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "email");
            if (twofa == null)
            {
                twofa = new TwoFactorAuth
                {
                    TwoFactorId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Provider = "email",
                    IsEnabled = true
                };
                _db.TwoFactorAuths.Add(twofa);
            }
            else
            {
                twofa.IsEnabled = true;
            }
            await _db.SaveChangesAsync();
            return Ok("2FA enabled");
        }

        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFa(DisableTwoFaRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("User not found");

            var twofa = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "email");
            if (twofa == null)
                return Ok("2FA disabled");

            twofa.IsEnabled = false;
            await _db.SaveChangesAsync();
            return Ok("2FA disabled");
        }
        [HttpPost("2fa/totp/setup")]
        public async Task<IActionResult> TotpSetup(EnableTwoFaRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("User not found");

            var secret = TotpService.GenerateSecret();
            var issuer = _config["Jwt:Issuer"] ?? "Auth.API";
            var uri = TotpService.BuildOtpAuthUri(issuer, user.Email, secret);

            var twofa = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "totp");
            if (twofa == null)
            {
                twofa = new TwoFactorAuth
                {
                    TwoFactorId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Provider = "totp",
                    SecretKey = secret,
                    IsEnabled = false
                };
                _db.TwoFactorAuths.Add(twofa);
            }
            else
            {
                twofa.SecretKey = secret;
                twofa.IsEnabled = false;
            }
            await _db.SaveChangesAsync();

            return Ok(new TotpSetupResponse { SecretKey = secret, OtpAuthUri = uri });
        }

        [HttpPost("2fa/totp/verify-setup")]
        public async Task<IActionResult> TotpVerifySetup(TotpVerifySetupRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("User not found");

            var twofa = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "totp");
            if (twofa == null || string.IsNullOrEmpty(twofa.SecretKey))
                return BadRequest("TOTP not setup");

            if (!TotpService.Verify(twofa.SecretKey!, request.Code))
                return BadRequest("Invalid TOTP code");

            twofa.IsEnabled = true;
            await _db.SaveChangesAsync();
            return Ok("TOTP enabled");
        }
        [HttpPost("2fa/totp/disable")]
        public async Task<IActionResult> TotpDisable(DisableTwoFaRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("User not found");

            var twofa = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "totp");
            if (twofa == null)
                return Ok("TOTP disabled");

            twofa.IsEnabled = false;
            twofa.SecretKey = null;
            await _db.SaveChangesAsync();
            return Ok("TOTP disabled");
        }
        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin(ExternalLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ProviderUserId))
                return BadRequest("Provider and ProviderUserId are required");

            var provider = request.Provider.ToLowerInvariant();
            if (provider != "google" && provider != "facebook")
                return BadRequest("Unsupported provider");

            // Find existing external login
            var ext = await _db.UserExternalLogins
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == request.ProviderUserId);

            User user;
            if (ext != null)
            {
                user = ext.User;
            }
            else
            {
                // Try match by email
                user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
                if (user == null)
                {
                    // Create new user
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        UserName = request.UserName ?? (request.Email ?? $"{provider}_{request.ProviderUserId}"),
                        Email = request.Email ?? string.Empty,
                        EmailConfirmed = !string.IsNullOrEmpty(request.Email),
                        IsExternalAccount = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(user);

                    _db.UserProfiles.Add(new UserProfile
                    {
                        UserId = user.UserId,
                        FirstName = request.FirstName ?? string.Empty,
                        LastName = request.LastName ?? string.Empty
                    });
                }

                // Link external login
                _db.UserExternalLogins.Add(new UserExternalLogin
                {
                    ExternalLoginId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Provider = provider,
                    ProviderUserId = request.ProviderUserId,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            // Issue tokens
            var (accessToken, refreshToken) = await _tokenService.CreateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new LoginResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null || !user.EmailConfirmed)
            {
                // Do not reveal existence
                return Ok("If the email exists, a reset link has been sent");
            }

            // Invalidate previous resets
            var existing = await _db.PasswordResets
                .Where(x => x.UserId == user.UserId && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            foreach (var r in existing)
            {
                r.ExpiresAt = DateTime.UtcNow; // expire old tokens
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashRefreshToken(token);
            var reset = new PasswordReset
            {
                ResetId = Guid.NewGuid(),
                UserId = user.UserId,
                ResetToken = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _db.PasswordResets.Add(reset);
            await _db.SaveChangesAsync();

            var minutes = Math.Max(1, (int)Math.Ceiling((reset.ExpiresAt - DateTime.UtcNow).TotalMinutes));
            var frontend = _config["FrontendUrl"] ?? "https://localhost:5173";
            var resetLink = $"{frontend.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            var body = $@"<h3>Password reset</h3>
<p>Click the link to reset your password:</p>
<p><a href=""{resetLink}"">Reset Password</a></p>
<p>Or use this token in the app: <b>{token}</b></p>
<p>Valid for {minutes} minutes.</p>";
            await _emailService.SendAsync(user.Email!, "Reset your password", body);

            return Ok("If the email exists, a reset link has been sent");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Email, token and new password are required");

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("Invalid token or email");

            var tokenHash = HashRefreshToken(request.Token);
            var reset = await _db.PasswordResets
                .Where(x => x.UserId == user.UserId && x.ResetToken == tokenHash && x.UsedAt == null)
                .OrderByDescending(x => x.ExpiresAt)
                .FirstOrDefaultAsync();

            if (reset == null)
                return BadRequest("Invalid token or email");

            if (reset.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Token expired");

            // Update password
            var (hash, salt) = PasswordService.Hash(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;
            user.SecurityStamp = Guid.NewGuid();

            reset.UsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok("Password has been reset");
        }
        private static string HashRefreshToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required");

            var hash = HashRefreshToken(request.RefreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null);

            if (token == null)
                return Ok(); // do not leak

            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok("Logged out");
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest("Email or Username is required");

            var user = await _db.Users.FirstOrDefaultAsync(x =>
                (!string.IsNullOrEmpty(request.Email) && x.Email == request.Email) ||
                (!string.IsNullOrEmpty(request.UserName) && x.UserName == request.UserName));

            // Do not reveal existence
            if (user == null)
                return BadRequest("Invalid credentials");

            if (!user.IsActive || !user.EmailConfirmed)
                return BadRequest("Account is not active or email not verified");

            if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                return BadRequest("Account is locked. Try again later.");

            var valid = PasswordService.Verify(request.Password, user.PasswordHash ?? string.Empty, user.PasswordSalt ?? string.Empty);

            if (!valid)
            {
                user.AccessFailedCount += 1;
                if (user.AccessFailedCount >= 5)
                {
                    user.IsLocked = true;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _db.SaveChangesAsync();
                return BadRequest("Invalid credentials");
            }

            // reset failed counter
            user.AccessFailedCount = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // If TOTP enabled, require code verification (no email)
            var twoFaTotp = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.IsEnabled && x.Provider == "totp");
            if (twoFaTotp != null)
            {
                return Ok(new LoginResponse
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    LastLoginAt = user.LastLoginAt,
                    RequiresTwoFactor = true,
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty
                });
            }

            // If 2FA via email enabled, send OTP and require verification
            var twoFaEmail = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.IsEnabled && x.Provider == "email");
            if (twoFaEmail != null)
            {
                // Invalidate previous 2FA codes
                var last2fa = await _db.UserVerifications
                    .Where(x => x.UserId == user.UserId && x.Type == "2FA")
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();
                if (last2fa != null)
                    last2fa.ExpiresAt = DateTime.UtcNow;

                var code = Random.Shared.Next(100000, 999999).ToString();
                var vf = new UserVerification
                {
                    UserId = user.UserId,
                    Type = "2FA",
                    Code = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                };
                _db.UserVerifications.Add(vf);
                await _db.SaveChangesAsync();

                var expiresInMinutes = Math.Max(1,
                    (int)Math.Ceiling((vf.ExpiresAt - DateTime.UtcNow).TotalMinutes));
                await _emailService.SendAsync(
                    user.Email!,
                    "Your 2FA code",
                    $"<h3>Two-factor code</h3><p><b>{code}</b></p><p>Valid for {expiresInMinutes} minutes</p>");

                return Ok(new LoginResponse
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    LastLoginAt = user.LastLoginAt,
                    RequiresTwoFactor = true,
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty
                });
            }

            var (accessToken, refreshToken) = await _tokenService.CreateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new LoginResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RequiresTwoFactor = false
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFa(TwoFaVerifyRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null)
                return BadRequest("Invalid user");

            // Check if TOTP is enabled
            var twofaTotp = await _db.TwoFactorAuths.FirstOrDefaultAsync(x => x.UserId == user.UserId && x.Provider == "totp" && x.IsEnabled);
            if (twofaTotp != null)
            {
                if (string.IsNullOrEmpty(twofaTotp.SecretKey) || !TotpService.Verify(twofaTotp.SecretKey!, request.Code))
                    return BadRequest("Invalid code");
            }
            else
            {
                var verification = await _db.UserVerifications
                    .Where(x => x.UserId == user.UserId && x.Type == "2FA" && x.Code == request.Code && x.VerifiedAt == null)
                    .OrderByDescending(x => x.ExpiresAt)
                    .FirstOrDefaultAsync();

                if (verification == null)
                    return BadRequest("Invalid code");

                if (verification.ExpiresAt < DateTime.UtcNow)
                    return BadRequest("Code expired");

                verification.VerifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var (accessToken, refreshToken) = await _tokenService.CreateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new LoginResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RequiresTwoFactor = false
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) &&
                string.IsNullOrEmpty(request.PhoneNumber))
                return BadRequest("Email or Phone is required");

            if (await _db.Users.AnyAsync(x =>
                x.Email == request.Email || x.PhoneNumber == request.PhoneNumber))
                return BadRequest("User already exists");

            // 1. Hash password
            var (hash, salt) = PasswordService.Hash(request.Password);

            // 2. Create user (CHƯA ACTIVE)
            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email ?? "",
                PhoneNumber = request.PhoneNumber,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            // 3. Profile
            _db.UserProfiles.Add(new UserProfile
            {
                UserId = user.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName
            });

            // 4. Verification code
            var code = Random.Shared.Next(100000, 999999).ToString();

            var verification = new UserVerification
            {
                UserId = user.UserId,
                Type = "Email",
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10) // chỉ set ở đây
            };

            _db.UserVerifications.Add(verification);
            await _db.SaveChangesAsync();

            var expiresInMinutes =
            Math.Max(1, (int)Math.Ceiling(
                (verification.ExpiresAt - DateTime.UtcNow).TotalMinutes
            ));


            await _emailService.SendAsync(
            request.Email!,
            "Verify your account",
            $"""
            <h3>Your verification code</h3>
            <p><b>{code}</b></p>
            <p>Valid for <b>{expiresInMinutes} minutes</b></p>
            """
);
            return Ok("Verification code sent");
        }
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
                return BadRequest("User not found");

            if (user.EmailConfirmed)
                return BadRequest("Email already verified");

            var verification = await _db.UserVerifications
                .Where(x =>
                    x.UserId == user.UserId &&
                    x.Type == "Email" &&
                    x.Code == request.Code &&
                    x.VerifiedAt == null)
                .OrderByDescending(x => x.ExpiresAt)
                .FirstOrDefaultAsync();

            if (verification == null)
                return BadRequest("Invalid verification code");

            if (verification.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Verification code expired");

            // ✅ Verify
            verification.VerifiedAt = DateTime.UtcNow;
            user.EmailConfirmed = true;
            user.IsActive = true;

            await _db.SaveChangesAsync();

            return Ok("Email verified successfully");
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(ResendOtpRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
                return Ok(); // ❗ không leak info

            if (user.EmailConfirmed)
                return BadRequest("Email already verified");

            // ⏱️ Cooldown: 60s
            var lastOtp = await _db.UserVerifications
                .Where(x =>
                    x.UserId == user.UserId &&
                    x.Type == "Email")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null &&
                (DateTime.UtcNow - lastOtp.CreatedAt).TotalSeconds < 60)
            {
                return BadRequest("Please wait before requesting a new code");
            }

            // ❌ Invalidate OTP cũ
            if (lastOtp != null)
                lastOtp.ExpiresAt = DateTime.UtcNow;

            // 🔐 OTP mới
            var code = Random.Shared.Next(100000, 999999).ToString();

            var verification = new UserVerification
            {
                UserId = user.UserId,
                Type = "Email",
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _db.UserVerifications.Add(verification);
            await _db.SaveChangesAsync();

            // ⏱️ Tính phút từ DB
            var expiresInMinutes = Math.Max(1,
                (int)Math.Ceiling(
                    (verification.ExpiresAt - DateTime.UtcNow).TotalMinutes));

            // 📧 Send mail
            await _emailService.SendAsync(
                user.Email!,
                "Resend verification code",
                $"""
        <h3>Your new verification code</h3>
        <b>{code}</b>
        <p>Valid for {expiresInMinutes} minutes</p>
        """
            );

            return Ok("Verification code resent");
        }


    }
}
