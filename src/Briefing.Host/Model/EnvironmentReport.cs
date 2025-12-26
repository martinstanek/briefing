using System.Collections.Generic;
using System.Text;

namespace Briefing.Host.Model;

public sealed record EnvironmentReport
{
    public required string Name { get; init; }
    
    public required int ExceptionCount { get; init; }

    public required IReadOnlyCollection<FailedRequest> TopFailedRequests { get; init; }

    public required IReadOnlyCollection<AppException> TopExceptions { get; init; }

    public required ResponseCodesSummary ResponsesSummary { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Collector: {Name}");
        sb.AppendLine();
        sb.AppendLine($"Failed Requests ({ResponsesSummary}):");

        foreach (var failedRequest in TopFailedRequests)
        {
            sb.AppendLine($"    {failedRequest.ResponseCode} - {failedRequest.Count}x - {failedRequest.App} - {failedRequest.Url}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"Top {ExceptionCount} Exceptions:");

        foreach (var exception in TopExceptions)
        {
            sb.AppendLine($"    {exception.Count}x - {exception.Name}");
        }

        return sb.ToString();
    }
}