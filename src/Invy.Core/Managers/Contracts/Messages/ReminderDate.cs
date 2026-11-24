namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A warranty, upkeep or other reminder date of one item.</summary>
/// <param name="Item">The item.</param>
/// <param name="FieldId">The date field it is in.</param>
/// <param name="Field">The field's label, such as Warranty until.</param>
/// <param name="On">The date.</param>
/// <param name="Upkeep">True when the field repeats, so it can be marked done.</param>
/// <param name="Days">Days from today: negative when it has passed.</param>
public sealed record ReminderDate(ItemSummary Item, Guid FieldId, string Field, DateOnly On, bool Upkeep, int Days);
