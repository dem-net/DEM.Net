using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core;

/// <summary>
/// Runs one operation per key at a time: concurrent callers with the same key share the same task.
/// The operation runs without the callers' cancellation token, so that a caller giving up does not fail the
/// others; each caller only stops waiting. A failed operation is forgotten and runs again on the next call.
/// </summary>
/// <param name="keepCompleted">
/// True: a completed operation is remembered and never runs again for this key (one time setup).
/// False: the key is released once the operation completes (in-flight deduplication, e.g. downloads).
/// </param>
internal sealed class SingleFlight(bool keepCompleted)
{
    private readonly ConcurrentDictionary<string, Task> _operations = new(StringComparer.Ordinal);

    public Task RunAsync(string key, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = _operations.GetOrAdd(key, completion.Task);
        if (task == completion.Task)
            _ = RunAndReleaseAsync(key, completion, operation); // this caller started the operation
        return task.WaitAsync(cancellationToken);
    }

    private async Task RunAndReleaseAsync(string key, TaskCompletionSource completion, Func<Task> operation)
    {
        // only this operation is removed, never one started since by another caller
        var entry = new KeyValuePair<string, Task>(key, completion.Task);
        try
        {
            await Task.Run(operation).ConfigureAwait(false);
            if (!keepCompleted)
                _operations.TryRemove(entry);
            completion.SetResult();
        }
        catch (Exception ex)
        {
            _operations.TryRemove(entry);
            completion.SetException(ex);
        }
    }

    /// <summary>True when the operation of this key completed successfully (only meaningful with keepCompleted).</summary>
    public bool IsCompleted(string key)
        => _operations.TryGetValue(key, out var task) && task.IsCompletedSuccessfully;

    public void Clear() => _operations.Clear();
}
