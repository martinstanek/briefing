using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.ApplicationInsights;

namespace Briefing.Host;

public class TelemetryChecker
{
    private readonly CheckerConfiguration _configuration;

    public TelemetryChecker(CheckerConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<BriefingReport> GetReportAsync(DateTime from, DateTime to, int topExceptions)
    {
        var credential = new ClientSecretCredential
        (
            tenantId: _configuration.TenantId.ToString(),
            clientId: _configuration.ClientId.ToString(),
            clientSecret: _configuration.Secret
        );

        var armClient = new Azure.ResourceManager.ArmClient(credential);
        var resource = new ResourceIdentifier(_configuration.ApplicationInsightsResourceId);
        var insights = armClient.GetApplicationInsightsComponentResource(resource);

        if (!insights.HasData)
        {
            throw new InvalidOperationException();
        }

        await Task.Yield();

        return new BriefingReport
        {
            TopExceptions = [],
            TopFailedRequests = []
        };
    }
}

public sealed record CheckerConfiguration
{
    public required Guid ClientId { get; init; }

    public required Guid TenantId { get; init; }

    public required string Secret { get; init; }

    public required string ApplicationInsightsResourceId { get; init; }
}

public sealed record BriefingReport
{
    public required IReadOnlyCollection<FailedRequest> TopFailedRequests { get; init; }

    public required IReadOnlyCollection<DetectedException> TopExceptions { get; init; }
}

public sealed record FailedRequest
{
    public required string Url { get; init; }

    public required string CallStack { get; init; }

    public required int ResponseCode { get; init; }

    public required int Count { get; init; }

    public required string Link { get; init; }
}

public sealed record DetectedException
{
    public required string Name { get; init; }

    public required int Count { get; init; }

    public required string Link { get; init; }
}