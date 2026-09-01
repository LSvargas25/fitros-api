using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;

namespace FitRos.Domain.Entities.Users;

public sealed class User : AggregateRoot, ITenantEntity
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    public bool EmailVerified { get; private set; }
    public string? EmailVerificationCodeHash { get; private set; }
    public DateTime? EmailVerificationCodeExpiresAtUtc { get; private set; }

    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    private User() { }

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

        // Every existing creation path is staff- or system-driven (an admin/
        // coach adding someone, the owner seed) - there's no one to send a
        // code to confirm, so those accounts start verified. Self-service
        // sign-up is the one path that needs to prove the email first; it
        // goes through CreateUnverified instead.
        EmailVerified = true;

        Role = role;
        Status = UserStatus.Active;

        CreatedAt = DateTime.UtcNow;
    }

    private static void ValidateCommon(
        string email,
        string firstName,
        string lastName,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("FirstName cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("LastName cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash cannot be empty.");
    }

    public static User Create(
     string email,
     string firstName,
     string lastName,
     string passwordHash,
     UserRole role)
    {
        if (role == UserRole.OwnerApp)
            throw new DomainException("OwnerApp cannot be created through this method.");

        ValidateCommon(email, firstName, lastName, passwordHash);

        var user = new User(
            Guid.NewGuid(),
            email.Trim(),
            firstName.Trim(),
            lastName.Trim(),
            passwordHash.Trim(),
            role);

        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, email)); 

        return user;
    }

    /// <summary>
    /// Self-service sign-up: same shape as Create, but the account starts
    /// unverified until the emailed code is confirmed.
    /// </summary>
    public static User CreateUnverified(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role)
    {
        var user = Create(email, firstName, lastName, passwordHash, role);
        user.EmailVerified = false;
        return user;
    }

    public static User CreateForGym(
        Guid gymId,
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role)
    {
        if (role == UserRole.OwnerApp)
            throw new DomainException("OwnerApp cannot be assigned to a gym.");

        ValidateCommon(email, firstName, lastName, passwordHash);

        var user = new User(
            Guid.NewGuid(),
            email.Trim(),
            firstName.Trim(),
            lastName.Trim(),
            passwordHash.Trim(),
            role);

        user.AssignToGym(gymId);

        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, email));

        return user;
    }

    internal static User CreateOwnerApp(
        Guid id,
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        DateTime? createdAtUtc = null)
    {
        ValidateCommon(email, firstName, lastName, passwordHash);

        var owner = new User(
            id,
            email.Trim(),
            firstName.Trim(),
            lastName.Trim(),
            passwordHash.Trim(),
            UserRole.OwnerApp);

        // When this instance feeds EF's HasData seed, the caller pins a constant
        // timestamp: a computed DateTime.UtcNow (the constructor default) would
        // make the model differ from the snapshot on every build, i.e. perpetual
        // migration drift.
        if (createdAtUtc.HasValue)
            owner.CreatedAt = createdAtUtc.Value;

        return owner;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        EnsureActive();
        SetProfile(firstName, lastName);
        Touch();
    }

    public void AssignToGym(Guid gymId)
    {
        if (Role == UserRole.OwnerApp)
            throw new InvalidOperationException("OwnerApp cannot be assigned to a gym.");

        if (gymId == Guid.Empty)
            throw new DomainException("GymId cannot be empty.");

        GymId = gymId;
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

        if (newRole == UserRole.OwnerApp)
            throw new DomainException("Cannot assign OwnerApp role.");

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

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Reset token hash cannot be empty.");

        PasswordResetTokenHash = tokenHash.Trim();
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;

        Touch();
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;

        Touch();
    }

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

    public void SetEmailVerificationCode(string codeHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new DomainException("Verification code hash cannot be empty.");

        EmailVerificationCodeHash = codeHash.Trim();
        EmailVerificationCodeExpiresAtUtc = expiresAtUtc;

        Touch();
    }

    public bool HasValidEmailVerificationCode(string codeHash, DateTime nowUtc)
    {
        if (EmailVerificationCodeHash is null)
            return false;

        if (EmailVerificationCodeExpiresAtUtc is null)
            return false;

        if (EmailVerificationCodeExpiresAtUtc < nowUtc)
            return false;

        return EmailVerificationCodeHash == codeHash;
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAtUtc = null;

        Touch();
    }
}