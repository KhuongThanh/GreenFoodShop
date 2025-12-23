namespace Auth.API.Entities
{
    public class UserProfile
    {
        public Guid ProfileId { get; set; }
        public Guid UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; }
        public User User { get; set; } = null!;
    }
}
