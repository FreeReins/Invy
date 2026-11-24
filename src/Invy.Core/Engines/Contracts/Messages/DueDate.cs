using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines.Contracts.Messages;

/// <summary>A reminder date: the value of one of an item's date fields marked to remind.</summary>
/// <param name="Space">The item's space.</param>
/// <param name="Item">The item.</param>
/// <param name="Field">The date field, with how many days ahead it reminds and whether it repeats.</param>
/// <param name="On">The date it holds.</param>
internal sealed record DueDate(Space Space, Item Item, TypeField Field, DateOnly On);
