namespace UserService.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;      // "Employee" or "Manager"
    public int? ManagerId { get; set; }                   // null if user is a Manager
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
