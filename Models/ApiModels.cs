namespace CleanEverydayMobile.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class LocationItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UserLocationResponse
{
    public string UserId { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class Checklist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<ChecklistItem> Items { get; set; } = new();
}

public class ChecklistItem
{
    public string Id { get; set; } = string.Empty;
    public string ChecklistId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Status { get; set; }
}

public class Printer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
