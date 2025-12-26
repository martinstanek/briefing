using System;

namespace Briefing.Host.Configuration;

public sealed record CheckerEnvironmentConfiguration
{
    public required Guid CollectorId { get; init; }

    public required string Name { get; init; }
}