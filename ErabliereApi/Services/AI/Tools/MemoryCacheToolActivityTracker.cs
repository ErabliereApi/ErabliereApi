using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Default <see cref="IToolActivityTracker" />, keeping the activity of a prompt
/// in the memory of the instance answering it.
/// </summary>
/// <remarks>
/// In memory on purpose: the status is progress feedback with a lifetime of a few
/// seconds, and losing it costs a label, not an answer. Behind a load balancer
/// spreading a user's requests over several instances, the poll may find nothing and
/// the chat falls back to its plain "ErabliereAI réfléchit…" — which is exactly the
/// phase 7 behaviour. Moving to <c>IDistributedCache</c> would be the fix if that
/// ever becomes worth a round trip to Redis.
/// </remarks>
public class MemoryCacheToolActivityTracker : IToolActivityTracker
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<ErabliereAiToolOptions> _options;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public MemoryCacheToolActivityTracker(IMemoryCache cache, IOptions<ErabliereAiToolOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    /// <inheritdoc />
    public void Publish(Guid? activityId, ToolActivityStep step)
    {
        if (activityId is null || activityId == Guid.Empty)
        {
            return;
        }

        var current = _cache.Get<ToolActivity>(CacheKey(activityId.Value));

        var steps = current is null ? new List<ToolActivityStep>() : [.. current.Steps];

        steps.Add(step);

        Set(activityId.Value, new ToolActivity(steps, Completed: false));
    }

    /// <inheritdoc />
    public void Complete(Guid? activityId)
    {
        if (activityId is null || activityId == Guid.Empty)
        {
            return;
        }

        var current = _cache.Get<ToolActivity>(CacheKey(activityId.Value));

        Set(activityId.Value, new ToolActivity(current?.Steps ?? [], Completed: true));
    }

    /// <inheritdoc />
    public ToolActivity? Get(Guid activityId)
    {
        return _cache.Get<ToolActivity>(CacheKey(activityId));
    }

    private void Set(Guid activityId, ToolActivity activity)
    {
        _cache.Set(CacheKey(activityId), activity, _options.Value.ActivityRetention);
    }

    private static string CacheKey(Guid activityId)
    {
        return $"erabliereai:activity:{activityId}";
    }
}
