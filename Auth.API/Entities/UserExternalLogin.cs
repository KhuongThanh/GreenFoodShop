namespace Auth.API.Entities
{
    public class UserExternalLogin
    {
        public Guid ExternalLoginId { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public string ProviderUserId { get; set; } = null!;
        public string? ProviderEmail { get; set; }
        public string? AccessTokenHash { get; set; }
        public string? RefreshTokenHash { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }


        
        public AuthProvider AuthProvider { get; set; } = null!;
    }
}
