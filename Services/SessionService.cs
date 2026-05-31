namespace CleanEverydayMobile.Services;

public class SessionService
{
    private readonly ILogger<SessionService> _logger;

    public string? UserId { get; private set; }
    public string? Fullname { get; private set; }
    public string? Location { get; private set; }

    public bool IsLoggedIn => UserId != null;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public void SetSession(string userId, string fullname, string? location)
    {
        _logger.LogInformation("Session set for userId: {UserId}", userId);
        UserId = userId;
        Fullname = fullname;
        Location = location;
    }

    public void SetLocation(string location)
    {
        _logger.LogInformation("Session location updated to: {Location}", location);
        Location = location;
    }

    public void Clear()
    {
        _logger.LogInformation("Session cleared for userId: {UserId}", UserId);
        UserId = null;
        Fullname = null;
        Location = null;
    }
}
