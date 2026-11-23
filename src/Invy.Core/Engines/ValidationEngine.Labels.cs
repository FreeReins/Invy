using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>QR labels.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The label with this code, in any space.</summary>
    public async Task<Label> LabelAsync(string code, CancellationToken cancellationToken) =>
        await labels.FindAsync(code, cancellationToken)
        ?? throw new NotFoundException($"There is no label {code}. Check the code, or scan it again.");

    /// <summary>The label with this code, which must be in this space.</summary>
    public async Task<Label> LabelAsync(Guid spaceId, string code, CancellationToken cancellationToken)
    {
        var label = await LabelAsync(code, cancellationToken);
        return label.SpaceId == spaceId
            ? label
            : throw new NotFoundException($"Label {code} belongs to another space. Scan it again to open that space.");
    }

    /// <summary>The number of labels to make in one batch.</summary>
    public static int LabelCount(int count) =>
        count is >= 1 and <= LabelEngine.BatchLimit
            ? count
            : throw new RuleViolationException($"Make 1 to {LabelEngine.BatchLimit} labels at a time.");
}
