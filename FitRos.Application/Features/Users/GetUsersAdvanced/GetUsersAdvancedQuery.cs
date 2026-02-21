using MediatR;

namespace FitRos.Application.Features.Users.GetUsersAdvanced;

public sealed record GetUsersAdvancedQuery(
    string? Cursor,
    int PageSize = 10,
    string? SortBy = "createdAt",
    string? SortDirection = "desc",
    int? Role = null,
    int? Status = null,
    string? Search = null,
    bool IncludeInactive = false
) : IRequest<CursorPagedResponse<UserListItemResponse>>;