namespace Auth.API.Entities
{
    public class UserVerification
    {
        public Guid VerificationId { get; set; }
        public Guid UserId { get; set; }

        public string Type { get; set; } = null!; // Email | Phone
        public string Code { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
    }

}
