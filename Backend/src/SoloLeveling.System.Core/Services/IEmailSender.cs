namespace SoloLeveling.System.Core.Services;

public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default);
}
