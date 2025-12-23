namespace Auth.API.Contracts
{
    public class TotpSetupResponse
    {
        public string SecretKey { get; set; } = null!;
        public string OtpAuthUri { get; set; } = null!;
    }
}
