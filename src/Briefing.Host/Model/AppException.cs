namespace Briefing.Host.Model;

public sealed record AppException
{
    public required string Name { get; init; }

    public required int Count { get; init; }
}