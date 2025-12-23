namespace Auth.API.Contracts
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
