using FitRos.Application.Abstractions.Messaging;

namespace FitRos.Tests.TestDoubles;

public class FakeEmailSender : IEmailSender
{
    public sealed record SentEmail(string To, string Subject, string HtmlBody);

    public List<SentEmail> SentEmails { get; } = new();

    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        SentEmails.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
