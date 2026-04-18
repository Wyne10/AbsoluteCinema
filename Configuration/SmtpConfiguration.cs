namespace AbsoluteCinema.Configuration;

public sealed class SmtpConfiguration
{
    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public string FromName { get; set; } = "AbsoluteCinema";
    public bool UseSsl { get; set; } = true;
}
