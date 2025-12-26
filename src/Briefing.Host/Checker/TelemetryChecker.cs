using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Monitor.Query;
using Briefing.Host.Config;
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
        var environmentReports = new List<EnvironmentReport>();
        
        foreach (var checkerEnvironment in _configuration.Environments)
        {
            var failedRequests = await GetRequestsAsync(from, to, checkerEnvironment.CollectorId, _configuration.WorkspaceId);
            var topEceptions = await GetTopExceptionsAsync(from, to, checkerEnvironment.CollectorId, _configuration.WorkspaceId);
            var responsesSummary = GetResponsesSummary(failedRequests, _configuration.CheckedResponseCodes.ToArray());
            var report = new EnvironmentReport
            {
                ExceptionCount = _configuration.TopExceptionCount,
                Name = checkerEnvironment.Name,
                ResponsesSummary = responsesSummary,
                TopExceptions = topEceptions,
                TopFailedRequests = failedRequests
            };
            
            environmentReports.Add(report);
        }
        
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
             | order by Count
             | take {_configuration.TopExceptionCount}
             """;
            
        var exceptionsResult = await _logsClient.Value.QueryWorkspaceAsync(
            workspaceId.ToString(),
            exceptionsQuery,
            new QueryTimeRange(from, to));

        if (exceptionsResult.Value.Status == Azure.Monitor.Query.Models.LogsQueryResultStatus.Success)
        {
            foreach (var row in exceptionsResult.Value.AllTables.FirstOrDefault()?.Rows ?? [])
            {
                var exception = new AppException()
                {
                    Count = int.Parse(row[1].ToString() ?? "0"),
                    Name = row[0].ToString() ?? "Unknown"
                };
                    
                topExceptionsList.Add(exception);
            }
        }

        return topExceptionsList;
    }

    private async Task<IReadOnlyCollection<FailedRequest>> GetRequestsAsync(DateTime from, DateTime to, Guid collectorId, Guid workspaceId)
    {
        var topFailedRequests = new List<FailedRequest>();
        var failedRequestsQuery = 
            $"""
             AppRequests
             | where ResourceGUID == '{collectorId.ToString()}'
             | where TimeGenerated >= datetime({from:O}) and TimeGenerated <= datetime({to:O})
             | where ResultCode  in ('503', '400')
             | summarize Count = count() by ResultCode, Url, AppRoleName
             | order by ResultCode, Count
             """;
        
        var failedRequestsResult = await _logsClient.Value.QueryWorkspaceAsync(
            workspaceId.ToString(),
            failedRequestsQuery, 
            new QueryTimeRange(from, to));
        
        if (failedRequestsResult.Value.Status == Azure.Monitor.Query.Models.LogsQueryResultStatus.Success)
        {
            foreach (var row in failedRequestsResult.Value.AllTables.FirstOrDefault()?.Rows ?? [])
            {
                var request = new FailedRequest
                {
                    ResponseCode = int.Parse(row[0].ToString() ?? "0"),
                    Url = row[1].ToString() ?? "",
                    App = row[2].ToString() ?? "",
                    Count = int.Parse(row[3].ToString() ?? "0")
                };
                
                topFailedRequests.Add(request);
            }
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