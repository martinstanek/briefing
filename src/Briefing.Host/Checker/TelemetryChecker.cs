using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Monitor.Query;
using Briefing.Host.Configuration;
using Briefing.Host.Model;

namespace Briefing.Host.Checker;

public class TelemetryChecker
{
    private readonly CheckerConfiguration _configuration;
    private readonly Lazy<LogsQueryClient> _logsClient;

    public TelemetryChecker(CheckerConfiguration configuration)
    {
        _logsClient = new Lazy<LogsQueryClient>(GetClient);
        _configuration = configuration;
    }
    
    public async Task<BriefingReport> GetReportAsync(DateTime from, DateTime to)
    {
        var environmentReportsTasks = _configuration.Environments.Select(async checkerEnvironment =>
        {
            var failedRequestsTask = GetRequestsAsync(from, to, checkerEnvironment.CollectorId, _configuration.WorkspaceId, _configuration.CheckedResponseCodes);
            var topExceptionsTask = GetTopExceptionsAsync(from, to, checkerEnvironment.CollectorId, _configuration.WorkspaceId);

            await Task.WhenAll(failedRequestsTask, topExceptionsTask);

            var failedRequests = await failedRequestsTask;
            var topExceptions = await topExceptionsTask;

            var responsesSummary = GetResponsesSummary(failedRequests, [.. _configuration.CheckedResponseCodes]);

            return new EnvironmentReport
            {
                ExceptionCount = _configuration.TopExceptionCount,
                Name = checkerEnvironment.Name,
                ResponsesSummary = responsesSummary,
                TopExceptions = topExceptions,
                TopFailedRequests = failedRequests,
                ListedStatusCodesDetails = _configuration.ListedResponseCodes
            };
        }).ToArray();

        var environmentReports = await Task.WhenAll(environmentReportsTasks);
        
        return new BriefingReport
        {
            From = from,
            To = to,
            EnvironmentReports = environmentReports
        };
    }

    private async Task<IReadOnlyCollection<AppException>> GetTopExceptionsAsync(DateTime from, DateTime to, Guid collectorId, Guid workspaceId)
    {
        var topExceptionsList = new List<AppException>();
        var exceptionsQuery =
            $"""
             AppExceptions
             | where ResourceGUID == '{collectorId.ToString()}'
             | where TimeGenerated >= datetime({from:O}) and TimeGenerated <= datetime({to:O})
             | summarize Count = count() by ExceptionType
             | order by Count desc
             | take {_configuration.TopExceptionCount}
             """;
            
        var exceptionsResult = await _logsClient.Value.QueryWorkspaceAsync(
            workspaceId.ToString(),
            exceptionsQuery,
            new QueryTimeRange(from, to));

        if (exceptionsResult.Value.Status != Azure.Monitor.Query.Models.LogsQueryResultStatus.Success)
        {
            throw new InvalidOperationException($"Query failed with status: {exceptionsResult.Value.Status}");
        }

        foreach (var row in exceptionsResult.Value.AllTables.FirstOrDefault()?.Rows ?? [])
        {
            var exception = new AppException()
            {
                Count = Convert.ToInt32(row[1] ?? 0),
                Name = row[0]?.ToString() ?? "Unknown"
            };

            topExceptionsList.Add(exception);
        }

        return topExceptionsList;
    }

    private async Task<IReadOnlyCollection<FailedRequest>> GetRequestsAsync(
        DateTime from, 
        DateTime to, 
        Guid collectorId, 
        Guid workspaceId, 
        IReadOnlyCollection<int> checkedResponseCodes)
    {
        var topFailedRequests = new List<FailedRequest>();
        var checkCodesParam = $"({string.Join(',', checkedResponseCodes)})";
        var failedRequestsQuery = 
            $"""
             AppRequests
             | where ResourceGUID == '{collectorId.ToString()}'
             | where TimeGenerated >= datetime({from:O}) and TimeGenerated <= datetime({to:O})
             | where ResultCode  in {checkCodesParam}
             | summarize Count = count() by ResultCode, Url, AppRoleName
             | order by ResultCode desc, Count desc
             """;
        
        var failedRequestsResult = await _logsClient.Value.QueryWorkspaceAsync(
            workspaceId.ToString(),
            failedRequestsQuery, 
            new QueryTimeRange(from, to));
        
        if (failedRequestsResult.Value.Status != Azure.Monitor.Query.Models.LogsQueryResultStatus.Success)
        {
            throw new InvalidOperationException($"Query failed with status: {failedRequestsResult.Value.Status}");
        }

        foreach (var row in failedRequestsResult.Value.AllTables.FirstOrDefault()?.Rows ?? [])
        {
            var responseCode = Convert.ToInt32(row[0] ?? 0);

            var request = new FailedRequest
            {
                ResponseCode = responseCode,
                Url = row[1]?.ToString() ?? "",
                App = row[2]?.ToString() ?? "",
                Count = Convert.ToInt32(row[3] ?? 0)
            };

            topFailedRequests.Add(request);
        }

        return topFailedRequests;
    }

    private static ResponseCodesSummary GetResponsesSummary(IReadOnlyCollection<FailedRequest> requests, int[] checkedResponseCodes)
    {
        var codes = new Dictionary<int, int>();

        foreach (var checkedResponseCode in checkedResponseCodes)
        {
            codes[checkedResponseCode] = 0;
        }

        foreach (var failedRequest in requests)
        {
            codes[failedRequest.ResponseCode] += failedRequest.Count;
        }

        return new ResponseCodesSummary
        {
            Codes = codes
        };
    }

    private static LogsQueryClient GetClient()
    {
        var tokenCredential = new ChainedTokenCredential(
            new AzureCliCredential(), 
            new VisualStudioCredential(), 
            new DefaultAzureCredential(), 
            new EnvironmentCredential());

        var options = new LogsQueryClientOptions
        {
            Audience = new LogsQueryAudience()
        };
        
        var logsClient = new LogsQueryClient(tokenCredential, options);

        return logsClient;
    }
}