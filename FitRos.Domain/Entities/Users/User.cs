using FitRos.Domain.Common;
using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Users;

public sealed class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { } // EF

    private User(
        Guid id,
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role)
    {
        Id = id;

        SetEmail(email);
        SetProfile(firstName, lastName);
        SetPasswordHash(passwordHash);

        Role = role;
        Status = UserStatus.Active;

        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("FirstName cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("LastName cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash cannot be empty.");

        return new User(
            Guid.NewGuid(),
            email.Trim(),
            firstName.Trim(),
            lastName.Trim(),
            passwordHash.Trim(),
            role);
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        EnsureActive();

        SetProfile(firstName, lastName);
        Touch();
    }

    public void UpdateBasicInfo(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeEmail(string email)
    {
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole newRole)
    {
        EnsureActive();

        if (Role == newRole)
            return;

        Role = newRole;
        Touch();
    }

    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            return;

        Status = UserStatus.Inactive;
        Touch();
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            return;

        Status = UserStatus.Active;
        Touch();
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        EnsureActive();

        SetPasswordHash(newPasswordHash);
        Touch();
    }

 

    private void EnsureActive()
    {
        if (Status != UserStatus.Active)
            throw new DomainException("User must be Active to perform this operation.");
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
    }

    private void SetProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("FirstName cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("LastName cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash cannot be empty.");

        PasswordHash = passwordHash.Trim();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}