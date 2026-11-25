using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Invy.Core.ResourceAccess;

/// <summary>
/// Reads a large JSON document from a stream one token, or one array element, at a time, so a backup with millions
/// of rows is restored without holding it in memory. Only the current element is buffered whole.
/// </summary>
internal sealed class JsonStreamReader(Stream stream, int bufferSize = 64 * 1024)
{
    private byte[] buffer = new byte[bufferSize];
    private int start;
    private int end;
    private bool final;
    private JsonReaderState state;

    public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;

    /// <summary>The current property name or string, or null for other tokens.</summary>
    public string? Text { get; private set; }

    /// <summary>Moves to the next token; false at the end of the document.</summary>
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (Damaged(TryRead))
            {
                return true;
            }

            if (final)
            {
                return false;
            }

            await FillAsync(cancellationToken);
        }
    }

    /// <summary>Skips the value after the current property name, however large, without keeping it.</summary>
    public async ValueTask SkipValueAsync(CancellationToken cancellationToken)
    {
        if (!await ReadAsync(cancellationToken) || TokenType is not (JsonTokenType.StartObject or JsonTokenType.StartArray))
        {
            return;
        }

        var depth = 1;
        while (depth > 0 && await ReadAsync(cancellationToken))
        {
            depth += TokenType switch
            {
                JsonTokenType.StartObject or JsonTokenType.StartArray => 1,
                JsonTokenType.EndObject or JsonTokenType.EndArray => -1,
                _ => 0,
            };
        }
    }

    /// <summary>
    /// Inside an array, reads its next element as <typeparamref name="T"/>; default at the array's end (which is then
    /// the current token).
    /// </summary>
    public async ValueTask<T?> ReadElementAsync<T>(JsonTypeInfo<T> json, CancellationToken cancellationToken)
    {
        while (true)
        {
            var (found, element) = Damaged(() => TryReadElement(json));
            if (found)
            {
                return element;
            }

            if (final)
            {
                throw new InvalidDataException("The backup file ends part way through.");
            }

            await FillAsync(cancellationToken);
        }
    }

    /// <summary>Runs one step of reading, turning a JSON error into the error a damaged backup gives.</summary>
    private static T Damaged<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The backup file is damaged or cut short, so it can't be read.", exception);
        }
    }

    /// <summary>The next element, or (true, default) at the array's end; (false, default) when more must be read first.</summary>
    private (bool Found, T? Element) TryReadElement<T>(JsonTypeInfo<T> json)
    {
        var reader = new Utf8JsonReader(buffer.AsSpan(start, end - start), final, state);
        if (!reader.Read())
        {
            return (false, default);
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            Advance(ref reader);
            return (true, default);
        }

        var elementStart = (int)reader.TokenStartIndex;
        if (!reader.TrySkip())
        {
            return (false, default);
        }

        var element = JsonSerializer.Deserialize(buffer.AsSpan(start + elementStart, (int)reader.BytesConsumed - elementStart), json);
        Advance(ref reader);
        return (true, element);
    }

    private bool TryRead()
    {
        var reader = new Utf8JsonReader(buffer.AsSpan(start, end - start), final, state);
        if (!reader.Read())
        {
            return false;
        }

        Text = reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.String ? reader.GetString() : null;
        Advance(ref reader);
        return true;
    }

    private void Advance(ref Utf8JsonReader reader)
    {
        TokenType = reader.TokenType;
        start += (int)reader.BytesConsumed;
        state = reader.CurrentState;
    }

    /// <summary>Keeps what hasn't been read yet, growing the buffer when one element fills it, and reads more after it.</summary>
    private async ValueTask FillAsync(CancellationToken cancellationToken)
    {
        var left = end - start;
        if (left == buffer.Length)
        {
            Array.Resize(ref buffer, buffer.Length * 2);
        }
        else if (start > 0)
        {
            Buffer.BlockCopy(buffer, start, buffer, 0, left);
        }

        start = 0;
        end = left;
        var read = await stream.ReadAsync(buffer.AsMemory(end), cancellationToken);
        end += read;
        final = read == 0;
    }
}
