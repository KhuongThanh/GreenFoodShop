namespace Auth.API.Contracts
{
    public class RegisterRequest
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }

}
