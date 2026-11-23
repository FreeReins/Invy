using Invy.Core.Engines;

namespace Invy.Core.Tests;

public class QuickAddTests
{
    private static readonly QuickAddEngine Engine = new();

    [Test]
    [Arguments("baseball, glove, 2 bats", "Baseball | Glove | Bats 2")]
    [Arguments("3.5 kg filament; screws x200\nwasher × 4", "Filament 3.5 kg | Screws 200 | Washer 4")]
    [Arguments("1 drill, 4 AA batteries, 2x gloves", "Drill | AA batteries 4 | Gloves 2")]
    [Arguments(" , ,\n", "")]
    [Arguments("Box 12", "Box 12")]
    public async Task Reads_names_and_amounts(string text, string expected)
    {
        var items = Engine.Parse(text);

        var read = string.Join(" | ", items.Select(item =>
            item.Quantity is null ? item.Name : $"{item.Name} {item.Quantity}{(item.Unit is null ? string.Empty : $" {item.Unit}")}"));
        await Assert.That(read).IsEqualTo(expected);
    }
}
