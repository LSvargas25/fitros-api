using FitRos.Domain.Common;
using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Users;

public sealed class User
{
    //Entity properties
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { } // EF

    // Private constructor for factory method
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
    // Factory method to create a new user
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
    // Methods to update user information
    public void UpdateProfile(string firstName, string lastName)
    {
        EnsureActive();

        SetProfile(firstName, lastName);
        Touch();
    }

    // Method to update basic info without checking active status (e.g., for admin updates)
    public void UpdateBasicInfo(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    // Method to change email without checking active status (e.g., for admin updates)
    public void ChangeEmail(string email)
    {
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
    // Method to change role without checking active status (e.g., for admin updates)
    public void ChangeRole(UserRole newRole)
    {
        EnsureActive();

        if (Role == newRole)
            return;

        Role = newRole;
        Touch();
    }
    // Methods to activate/deactivate user
    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            return;

        Status = UserStatus.Inactive;
        Touch();
    }
    // Method to activate user without checking current status (e.g., for admin reactivation)
    public void Activate()
    {
        if (Status == UserStatus.Active)
            return;

        Status = UserStatus.Active;
        Touch();
    }

    // Method to change password hash without checking active status (e.g., for password reset)
    public void ChangePasswordHash(string newPasswordHash)
    {
        EnsureActive();

        SetPasswordHash(newPasswordHash);
        Touch();
    }


    // Private helper methods to keep domain logic consistent and avoid code duplication
    private void EnsureActive()
    {
        if (Status != UserStatus.Active)
            throw new DomainException("User must be Active to perform this operation.");
    }

    // Private setters to encapsulate validation logic
    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
    }
    // Private method to set profile information with validation
    private void SetProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("FirstName cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("LastName cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
    // Private method to set password hash with validation
    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash cannot be empty.");

        PasswordHash = passwordHash.Trim();
    }
    // Private method to update the UpdatedAt timestamp
    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    // Methods related to password reset token management
    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Reset token hash cannot be empty.");

        PasswordResetTokenHash = tokenHash.Trim();
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;

        Touch();
    }
    // Method to clear password reset token without checking active status (e.g., after successful password reset)
    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;

        Touch();
    }
    // Method to validate a given password reset token against the stored hash and expiration
    public bool HasValidPasswordResetToken(string tokenHash, DateTime nowUtc)
    {
        if (PasswordResetTokenHash is null)
            return false;

        if (PasswordResetTokenExpiresAtUtc is null)
            return false;

        if (PasswordResetTokenExpiresAtUtc < nowUtc)
            return false;

        return PasswordResetTokenHash == tokenHash;
    }

}