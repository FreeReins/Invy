namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>What <see cref="Flag"/> checks.</summary>
internal enum ItemFlag
{
    Container,
    Counted,
    Unique,
    Active,
    Missing,
    Lent,
    Disposed,
    Archived,

    /// <summary>has:barcode, a value in one of the barcode fields named.</summary>
    HasBarcode,

    /// <summary>is:low, a counted item at or below its low-stock level, after reservations.</summary>
    Low,
}
