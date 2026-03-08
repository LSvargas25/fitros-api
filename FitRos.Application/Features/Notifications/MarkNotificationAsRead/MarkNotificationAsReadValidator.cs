using FluentValidation;

namespace FitRos.Application.Features.Notifications.MarkNotificationAsRead;

public class MarkNotificationAsReadValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty();
    }
}
