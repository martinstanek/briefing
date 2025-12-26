using System;
using System.Collections.Generic;
using System.Text;

namespace Briefing.Host.Model;

public sealed record BriefingReport
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public required IReadOnlyCollection<EnvironmentReport> EnvironmentReports { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"The telemetry report for : {From:d} - {To:d}");
        sb.AppendLine();
        
        foreach (var environmentReport in EnvironmentReports)
        {
            sb.AppendLine(environmentReport.ToString());
        }

        return sb.ToString();
    }
}