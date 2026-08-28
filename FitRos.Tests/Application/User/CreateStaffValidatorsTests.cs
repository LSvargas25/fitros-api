using FitRos.Application.Features.Users.Client.CreateClient;
using FitRos.Application.Features.Users.Coach.CreateCoach;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Users;

public class CreateStaffValidatorsTests
{
    [Theory]
    [InlineData("", "Ana", "Diaz", "secret1", false)]        // empty email
    [InlineData("not-an-email", "Ana", "Diaz", "secret1", false)]
    [InlineData("ana@fitros.com", "", "Diaz", "secret1", false)] // empty first name
    [InlineData("ana@fitros.com", "Ana", "Diaz", "123", false)]  // password too short
    [InlineData("ana@fitros.com", "Ana", "Diaz", "secret1", true)]
    public void CreateClientCommandValidator_enforces_the_basics(
        string email, string first, string last, string password, bool expectedValid)
    {
        var result = new CreateClientCommandValidator()
            .Validate(new CreateClientCommand(email, first, last, password));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("", "Ana", "Diaz", "secret1", false)]
    [InlineData("not-an-email", "Ana", "Diaz", "secret1", false)]
    [InlineData("ana@fitros.com", "Ana", "", "secret1", false)]
    [InlineData("ana@fitros.com", "Ana", "Diaz", "123", false)]
    [InlineData("ana@fitros.com", "Ana", "Diaz", "secret1", true)]
    public void CreateCoachCommandValidator_enforces_the_basics(
        string email, string first, string last, string password, bool expectedValid)
    {
        var result = new CreateCoachCommandValidator()
            .Validate(new CreateCoachCommand(email, first, last, password));

        result.IsValid.Should().Be(expectedValid);
    }
}
