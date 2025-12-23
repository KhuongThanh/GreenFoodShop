namespace Auth.API.Contracts
{
    public class ExternalLoginRequest
    {
        public string Provider { get; set; } = null!; // google or facebook
        public string ProviderUserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
