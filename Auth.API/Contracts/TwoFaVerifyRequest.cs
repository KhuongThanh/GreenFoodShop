namespace Auth.API.Contracts
{
    public class TwoFaVerifyRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
