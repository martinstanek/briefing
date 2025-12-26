using System.Collections.Generic;
using System.Linq;

namespace Briefing.Host.Model;

public sealed record ResponseCodesSummary
{
    public required IReadOnlyDictionary<int, int> Codes { get; init; }

    public override string ToString()
    {
        return string.Join(", ", Codes.Select(s => $"HTTP {s.Key} - {s.Value}x"));
    }
}