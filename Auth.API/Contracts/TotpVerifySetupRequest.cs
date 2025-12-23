namespace Auth.API.Contracts
{
    public class TotpVerifySetupRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
