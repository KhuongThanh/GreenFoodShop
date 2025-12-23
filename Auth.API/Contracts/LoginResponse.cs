namespace Auth.API.Contracts
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public bool RequiresTwoFactor { get; set; }
    }
}
