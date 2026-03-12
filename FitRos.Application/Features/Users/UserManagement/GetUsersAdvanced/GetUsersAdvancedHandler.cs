using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.UserManagement.GetUsersAdvanced;

public sealed class GetUsersAdvancedHandler
    : IRequestHandler<GetUsersAdvancedQuery, CursorPagedResponse<UserListItemResponse>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUsersAdvancedHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CursorPagedResponse<UserListItemResponse>> Handle(
        GetUsersAdvancedQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (request.IncludeInactive)
            query = query.IgnoreQueryFilters();

        query = query.ApplyUserVisibility(_currentUser);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToUpperInvariant();

            query = query.Where(u =>
                u.NormalizedEmail.StartsWith(s) ||
                u.FirstName.ToUpper().StartsWith(s) ||
                u.LastName.ToUpper().StartsWith(s));
        }

        if (request.Role.HasValue)
            query = query.Where(u => (int)u.Role == request.Role.Value);

        if (request.Status.HasValue)
            query = query.Where(u => (int)u.Status == request.Status.Value);

        var desc = request.SortDirection?.ToLowerInvariant() != "asc";

        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var parts = request.Cursor.Split('|');

            if (parts.Length != 2)
                throw new ArgumentException("Invalid cursor format.");

            var cursorDate = DateTime.ParseExact(
                parts[0],
                "O",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind);

            var cursorId = Guid.Parse(parts[1]);

            query = desc
                ? query.Where(u =>
                    u.CreatedAt < cursorDate ||
                    u.CreatedAt == cursorDate && u.Id < cursorId)
                : query.Where(u =>
                    u.CreatedAt > cursorDate ||
                    u.CreatedAt == cursorDate && u.Id > cursorId);
        }

        query = desc
            ? query.OrderByDescending(u => u.CreatedAt)
                   .ThenByDescending(u => u.Id)
            : query.OrderBy(u => u.CreatedAt)
                   .ThenBy(u => u.Id);

        var items = await query
            .Take(pageSize)
            .Select(u => new UserListItemResponse(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                (int)u.Role,
                (int)u.Status,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        string? nextCursor = null;

        if (items.Count == pageSize)
        {
            var lastItem = items.Last();
            nextCursor = $"{lastItem.CreatedAt:O}|{lastItem.Id}";
        }

        return new CursorPagedResponse<UserListItemResponse>(items, nextCursor);
    }
}