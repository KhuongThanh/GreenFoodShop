namespace Auth.API.Contracts
{
    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}
