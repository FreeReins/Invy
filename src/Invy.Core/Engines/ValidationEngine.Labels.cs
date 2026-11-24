using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>QR labels.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>
    /// The label with this code, in any space the person is a member of. A label in a space they aren't in is
    /// reported as missing, so codes can't be used to find out what other spaces hold.
    /// </summary>
    public async Task<Label> LabelAsync(string code, Guid userId, CancellationToken cancellationToken) =>
        await labels.FindAsync(code, cancellationToken) is { } label && await IsMemberAsync(label.SpaceId, userId, cancellationToken)
            ? label
            : throw NoSuchLabel(code);

    /// <summary>The label with this code, which must be in this space. One in another of the person's spaces says so.</summary>
    public async Task<Label> LabelAsync(Guid spaceId, string code, Guid userId, CancellationToken cancellationToken)
    {
        var label = await LabelAsync(code, userId, cancellationToken);
        return label.SpaceId == spaceId
            ? label
            : throw new NotFoundException($"Label {code} belongs to another space. Scan it again to open that space.");
    }

    private async Task<bool> IsMemberAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        (await spaces.FindAsync(spaceId, cancellationToken))?.Members.Any(member => member.UserId == userId) == true;

    private static NotFoundException NoSuchLabel(string code) => new($"There is no label {code}. Check the code, or scan it again.");

    /// <summary>The number of labels to make in one batch.</summary>
    public static int LabelCount(int count) =>
        count is >= 1 and <= LabelEngine.BatchLimit
            ? count
            : throw new RuleViolationException($"Make 1 to {LabelEngine.BatchLimit} labels at a time.");
}
