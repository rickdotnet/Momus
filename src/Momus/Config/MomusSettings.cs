namespace Momus.Config;

public class MomusSettings
{
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string? CredsFilePath { get; set; }
    public string? Jwt { get; set; }
    public string? NKey { get; set; }
    public string? Token { get; set; }
}