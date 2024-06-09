namespace Momus.Config;

public record MomusSettings
{
    public string StoreName { get; set; } = "momus";
    public string KeyName { get; set; } = "route-config";
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string? CredsFilePath { get; set; }
    public string? Jwt { get; set; }
    public string? NKey { get; set; }
    public string? Seed { get; set; }
    public string? Token { get; set; }
}