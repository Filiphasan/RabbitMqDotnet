namespace Shared.Common.Models;

public class ProjectSetting
{
    public const string SectionName = "Settings";

    public required RabbitMqSetting RabbitMq { get; set; }
}

public class RabbitMqSetting
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
}