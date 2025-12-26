namespace Briefing.Host.Model;

public sealed record FailedRequest
{
    public required int ResponseCode { get; init; }
    
    public required string App { get; init; }

    public required string Url { get; init; }    

    public required int Count { get; init; }
}