using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.GetUsersAdvanced;

public sealed class GetUsersAdvancedHandler
    : IRequestHandler<GetUsersAdvancedQuery, CursorPagedResponse<UserListItemResponse>>
{
    private readonly IFitRosDbContext _context;

    public GetUsersAdvancedHandler(IFitRosDbContext context)
    {
        _context = context;
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

        query = ApplySorting(query, request.SortBy, request.SortDirection);


        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var parts = request.Cursor.Split('|');

            if (parts.Length != 2)
                throw new ArgumentException("Invalid cursor format.");

            var cursorDate = DateTime.Parse(parts[0]);
            var cursorId = Guid.Parse(parts[1]);

            var desc = request.SortDirection?.ToLowerInvariant() != "asc";

            query = desc
     ? query.Where(u =>
         u.CreatedAt < cursorDate ||
         (u.CreatedAt == cursorDate && u.Id < cursorId))
     : query.Where(u =>
         u.CreatedAt > cursorDate ||
         (u.CreatedAt == cursorDate && u.Id > cursorId));
        }

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

    private static IQueryable<User> ApplySorting(
        IQueryable<User> query,
        string? sortBy,
        string? sortDirection)
    {
        var desc = sortDirection?.ToLowerInvariant() != "asc";

        return sortBy?.ToLowerInvariant() switch
        {
            "email" => desc
                ? query.OrderByDescending(u => u.Email).ThenByDescending(u => u.Id)
                : query.OrderBy(u => u.Email).ThenBy(u => u.Id),

            "firstname" => desc
                ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.Id)
                : query.OrderBy(u => u.FirstName).ThenBy(u => u.Id),

            "lastname" => desc
                ? query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.Id)
                : query.OrderBy(u => u.LastName).ThenBy(u => u.Id),

            _ => desc
                ? query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
                : query.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id),
        };
    }
}