using System.Net;

namespace ErabliereAPI.Proxy;

public class RetryPolicyHandler : DelegatingHandler
{
    public RetryPolicyHandler()
    {
        MaxRetires = 3;
        Delay = TimeSpan.FromSeconds(30);
    }

    public RetryPolicyHandler(int maxRetries, TimeSpan delay)
    {
        MaxRetires = maxRetries;
        Delay = delay;
    }

    public RetryPolicyHandler(int maxRetries, TimeSpan delay, Func<HttpResponseMessage, bool> shouldRetry)
    {
        MaxRetires = maxRetries;
        Delay = delay;
        ShouldRetry = shouldRetry;
    }

    public int MaxRetires { get; set; }
    public TimeSpan Delay { get; set; }
    public Func<HttpResponseMessage, bool> ShouldRetry { get; set; } = response => 
        response.StatusCode == HttpStatusCode.RequestTimeout || 
        response.StatusCode == HttpStatusCode.ServiceUnavailable || 
        response.StatusCode == HttpStatusCode.GatewayTimeout ||
        response.StatusCode == HttpStatusCode.InternalServerError ||
        response.StatusCode == HttpStatusCode.BadGateway;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int i = 0; i < MaxRetires; i++)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (ShouldRetry(response) && i < MaxRetires - 1)
                {
                    await Task.Delay(Delay, cancellationToken);
                    continue;
                }

                return response;
            }
            catch (HttpRequestException) when (i < MaxRetires - 1)
            {
                await Task.Delay(Delay, cancellationToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}