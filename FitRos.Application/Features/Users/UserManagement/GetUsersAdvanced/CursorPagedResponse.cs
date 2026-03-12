namespace FitRos.Application.Features.Users.UserManagement.GetUsersAdvanced;

public sealed class CursorPagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public string? NextCursor { get; }

    public CursorPagedResponse(IReadOnlyCollection<T> items, string? nextCursor)
    {
        Items = items;
        NextCursor = nextCursor;
    }
}