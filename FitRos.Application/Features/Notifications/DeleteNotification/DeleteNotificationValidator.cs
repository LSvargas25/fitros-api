using FluentValidation;

namespace FitRos.Application.Features.Notifications.DeleteNotification;

public class DeleteNotificationValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty();
    }
}
