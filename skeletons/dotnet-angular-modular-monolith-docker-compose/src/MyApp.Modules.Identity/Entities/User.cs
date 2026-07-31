namespace MyApp.Modules.Identity.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // 'admin' | 'agent'
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
