using System;
using System.Collections.Generic;

namespace Briefing.Host.Configuration;

public sealed record CheckerConfiguration
{
    public required Guid WorkspaceId { get; init; }

    public required int TopExceptionCount { get; init; }

    public required IReadOnlyCollection<int> CheckedResponseCodes { get; init; }

    public required IReadOnlyCollection<CheckerEnvironmentConfiguration> Environments { get; init; }
}