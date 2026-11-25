namespace Invy.Bench;

/// <summary>The messages the managers published, kept in memory so the benchmark can run their handlers itself.</summary>
internal sealed class BenchOutbox
{
    private readonly List<object> messages = [];

    public void Add(object message)
    {
        lock (messages)
        {
            messages.Add(message);
        }
    }

    /// <summary>The messages of type <typeparamref name="T"/> published so far, taken out of the outbox.</summary>
    public List<T> Take<T>()
    {
        lock (messages)
        {
            var taken = messages.OfType<T>().ToList();
            messages.RemoveAll(message => message is T);
            return taken;
        }
    }
}
