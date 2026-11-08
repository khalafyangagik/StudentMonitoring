using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Tests;

public sealed class AuthServiceTests
{
    [Theory]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("superadmin", true)]
    [InlineData("administrator", true)]
    [InlineData("ադմինիստրատոր", true)]
    [InlineData("student", false)]
    [InlineData("lecturer", false)]
    public void RequiresLoginCode_ReturnsExpectedResult(string role, bool expected)
    {
        var account = new AuthAccount(1, "Անուն", "Ազգանուն", "person@example.com", "hash", role, true, true);

        Assert.Equal(expected, AuthService.RequiresLoginCode(account));
    }
}
