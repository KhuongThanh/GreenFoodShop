namespace Auth.API.Entities
{
    public class PasswordReset
    {
        public Guid ResetId { get; set; }
        public Guid UserId { get; set; }
        public string ResetToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public User User { get; set; } = null!;
    }
}
