using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Users;

public class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public FitnessGoal Goal { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private User() { }

    private User(
        Guid id,
        string email,
        string firstName,
        string lastName,
        FitnessGoal goal)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Goal = goal;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(
        string email,
        string firstName,
        string lastName,
        FitnessGoal goal)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.");

        return new User(
            Guid.NewGuid(),
            email.Trim().ToLower(),
            firstName.Trim(),
            lastName.Trim(),
            goal);
    }

    public void ChangeGoal(FitnessGoal newGoal)
    {
        Goal = newGoal;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
