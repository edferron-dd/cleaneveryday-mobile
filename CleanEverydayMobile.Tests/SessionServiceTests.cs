using CleanEverydayMobile.Services;
using Moq;

namespace CleanEverydayMobile.Tests;

public class SessionServiceTests
{
    private static SessionService Build() =>
        new(new Mock<ILogger<SessionService>>().Object);

    [Fact]
    public void IsLoggedIn_ReturnsFalse_Initially()
    {
        var svc = Build();

        Assert.False(svc.IsLoggedIn);
    }

    [Fact]
    public void UserId_IsNull_Initially()
    {
        var svc = Build();

        Assert.Null(svc.UserId);
    }

    [Fact]
    public void SetSession_SetsAllProperties()
    {
        var svc = Build();

        svc.SetSession("u1", "Alice Smith", "Plant A");

        Assert.Equal("u1", svc.UserId);
        Assert.Equal("Alice Smith", svc.Fullname);
        Assert.Equal("Plant A", svc.Location);
    }

    [Fact]
    public void SetSession_SetsIsLoggedInTrue()
    {
        var svc = Build();

        svc.SetSession("u1", "Alice Smith", null);

        Assert.True(svc.IsLoggedIn);
    }

    [Fact]
    public void SetSession_AllowsNullLocation()
    {
        var svc = Build();

        svc.SetSession("u1", "Alice Smith", null);

        Assert.Null(svc.Location);
    }

    [Fact]
    public void SetLocation_UpdatesLocation()
    {
        var svc = Build();
        svc.SetSession("u1", "Alice Smith", null);

        svc.SetLocation("Plant B");

        Assert.Equal("Plant B", svc.Location);
    }

    [Fact]
    public void SetLocation_DoesNotAffectUserId()
    {
        var svc = Build();
        svc.SetSession("u1", "Alice Smith", null);

        svc.SetLocation("Plant B");

        Assert.Equal("u1", svc.UserId);
    }

    [Fact]
    public void Clear_ResetsAllProperties()
    {
        var svc = Build();
        svc.SetSession("u1", "Alice Smith", "Plant A");

        svc.Clear();

        Assert.Null(svc.UserId);
        Assert.Null(svc.Fullname);
        Assert.Null(svc.Location);
    }

    [Fact]
    public void Clear_SetsIsLoggedInFalse()
    {
        var svc = Build();
        svc.SetSession("u1", "Alice Smith", "Plant A");

        svc.Clear();

        Assert.False(svc.IsLoggedIn);
    }

    [Fact]
    public void Clear_OnFreshSession_DoesNotThrow()
    {
        var svc = Build();

        var ex = Record.Exception(() => svc.Clear());

        Assert.Null(ex);
    }
}
