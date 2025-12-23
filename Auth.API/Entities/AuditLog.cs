namespace Auth.API.Entities
{
    public class AuditLog
    {
        public Guid AuditId { get; set; }
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
