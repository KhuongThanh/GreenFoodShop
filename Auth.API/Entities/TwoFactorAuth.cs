namespace Auth.API.Entities
{
    public class TwoFactorAuth
    {
        public Guid TwoFactorId { get; set; }
        public Guid UserId { get; set; }
        public string? Provider { get; set; }
        public string? SecretKey { get; set; }
        public bool IsEnabled { get; set; }
        public User User { get; set; } = null!;
    }
}
