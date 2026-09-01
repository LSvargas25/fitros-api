using FitRos.Application.Abstractions.Messaging;

namespace FitRos.Tests.TestDoubles;

public class FakeEmailSender : IEmailSender
{
    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
