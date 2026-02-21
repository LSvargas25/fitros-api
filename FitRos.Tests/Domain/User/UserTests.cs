using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Domain.User
{
    public class UserTests
    {
        [Fact]
        public void Should_Create_User_With_Normalized_Email()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "hashed-password",
                UserRole.Client);

            user.Email.Should().Be("test@email.com");
            user.NormalizedEmail.Should().Be("TEST@EMAIL.COM");
            user.Status.Should().Be(UserStatus.Active);
            user.CreatedAt.Should().NotBe(default);
        }

        [Fact]
        public void Should_Not_Create_User_With_Empty_Email()
        {
            var act = () => FitRos.Domain.Entities.Users.User.Create(
                "",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            act.Should().Throw<DomainException>()
                .WithMessage("Email cannot be empty.");
        }

        [Fact]
        public void Should_Not_Create_User_With_Empty_PasswordHash()
        {
            var act = () => FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "",
                UserRole.Client);

            act.Should().Throw<DomainException>()
                .WithMessage("PasswordHash cannot be empty.");
        }

        [Fact]
        public void Should_Update_Profile_And_Set_UpdatedAt()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            user.UpdateProfile("Carlos", "Lopez");

            user.FirstName.Should().Be("Carlos");
            user.LastName.Should().Be("Lopez");
            user.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Should_Not_Update_When_User_Is_Inactive()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            user.Deactivate();

            var act = () => user.UpdateProfile("New", "Name");

            act.Should().Throw<DomainException>()
                .WithMessage("User must be Active to perform this operation.");
        }

        [Fact]
        public void Should_Deactivate_User()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            user.Deactivate();

            user.Status.Should().Be(UserStatus.Inactive);
            user.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Should_Change_Email_And_Normalize()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "old@email.com",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            user.ChangeEmail("NewEmail@Domain.com");

            user.Email.Should().Be("NewEmail@Domain.com");
            user.NormalizedEmail.Should().Be("NEWEMAIL@DOMAIN.COM");
        }

        [Fact]
        public void Should_Change_PasswordHash()
        {
            var user = FitRos.Domain.Entities.Users.User.Create(
                "test@email.com",
                "Luis",
                "Vargas",
                "hash",
                UserRole.Client);

            user.ChangePasswordHash("new-hash");

            user.PasswordHash.Should().Be("new-hash");
            user.UpdatedAt.Should().NotBeNull();
        }
    }
}