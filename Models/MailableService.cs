namespace AbsoluteCinema.Models;

public record MailableService(string Key, string DisplayName, MailableServiceCategory Category);

public enum MailableServiceCategory
{
    Report,
    Schedule
}
