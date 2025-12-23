namespace Auth.API.Entities
{
    public class AuthProvider
    {
        public string Provider { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
