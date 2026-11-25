using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Invy.Core.ResourceAccess;

namespace Invy.Core.Tests;

public class JsonStreamReaderTests
{
    private static readonly JsonTypeInfo<StreamRow> Row = (JsonTypeInfo<StreamRow>)JsonSerializerOptions.Web.GetTypeInfo(typeof(StreamRow));

    private const string Backup = """
        {
          "createdAt": "2026-01-01T00:00:00Z",
          "somethingNewer": { "nested": [1, [2, { "deep": "]}" }]], "x": null },
          "rows": [{ "name": "first", "notes": "a longer note that spans many small buffers" }, { "name": "second" }, { "name": "third" }],
          "after": "end"
        }
        """;

    [Test]
    [Arguments(1)]
    [Arguments(7)]
    [Arguments(64 * 1024)]
    public async Task Reads_rows_one_at_a_time_whatever_the_buffer_size(int bufferSize)
    {
        var reader = new JsonStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Backup)), bufferSize);
        var names = new List<string>();
        string? after = null;

        await reader.ReadAsync(CancellationToken.None);
        while (await reader.ReadAsync(CancellationToken.None) && reader.TokenType == JsonTokenType.PropertyName)
        {
            switch (reader.Text)
            {
                case "rows":
                    await reader.ReadAsync(CancellationToken.None);
                    while (await reader.ReadElementAsync(Row, CancellationToken.None) is { } row)
                    {
                        names.Add(row.Name);
                    }

                    break;
                case "after":
                    await reader.ReadAsync(CancellationToken.None);
                    after = reader.Text;
                    break;
                default:
                    await reader.SkipValueAsync(CancellationToken.None);
                    break;
            }
        }

        await Assert.That(names).IsEquivalentTo(["first", "second", "third"]);
        await Assert.That(after).IsEqualTo("end");
        await Assert.That(reader.TokenType).IsEqualTo(JsonTokenType.EndObject);
    }

    [Test]
    public async Task A_file_that_ends_part_way_through_a_row_is_refused()
    {
        var cut = Backup[..Backup.IndexOf("second", StringComparison.Ordinal)];
        var reader = new JsonStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(cut)), 8);
        while (await reader.ReadAsync(CancellationToken.None) && reader.Text != "rows")
        {
        }

        await reader.ReadAsync(CancellationToken.None);
        await reader.ReadElementAsync(Row, CancellationToken.None);

        await Assert.That(async () => await reader.ReadElementAsync(Row, CancellationToken.None)).Throws<InvalidDataException>();
    }
}
