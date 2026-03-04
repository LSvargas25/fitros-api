using FitRos.Domain.Common;

namespace FitRos.Domain.Entities.Gym;

public sealed class Gym
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;

    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Gym() { }

    public static Gym Create(
        string name,
        string address,
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Gym name cannot be empty.");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Gym address cannot be empty.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Gym phone number cannot be empty.");

        return new Gym
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Address = address.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void ChangeName(string name)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Gym name cannot be empty.");

        Name = name.Trim();
        Touch();
    }

    public void ChangeAddress(string address)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Gym address cannot be empty.");

        Address = address.Trim();
        Touch();
    }

    public void ChangePhoneNumber(string phoneNumber)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Gym phone number cannot be empty.");

        PhoneNumber = phoneNumber.Trim();
        Touch();
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (IsActive)
            return;

        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
            return;

        IsActive = false;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Deleted gym cannot be modified.");
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}