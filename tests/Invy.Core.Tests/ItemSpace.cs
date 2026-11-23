using System.Text.Json;
using Invy.Core.Managers;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>
/// A space for item tests: Owen owns it and Vera can only look. It has a Box type (holds items) and a Tool type with a
/// sortable brand and price and a power choice.
/// </summary>
internal sealed class ItemSpace
{
    private ItemSpace(TestCore core, Guid spaceId, ItemTypeResponse box, ItemTypeResponse tool)
    {
        Core = core;
        SpaceId = spaceId;
        Box = box;
        Tool = tool;
    }

    public TestCore Core { get; }

    public Guid SpaceId { get; }

    public ItemTypeResponse Box { get; }

    public ItemTypeResponse Tool { get; }

    public Guid Owen => Core.Accounts.User("owen").Id;

    public Guid Vera => Core.Accounts.User("vera").Id;

    public ItemManager Items => Core.ItemManager();

    public Guid Field(string key) => Tool.Fields.Single(field => field.Key == key).Id!.Value;

    public static async Task<ItemSpace> MakeAsync()
    {
        var core = new TestCore();
        core.Accounts.Users.Add(new UserAccount { Username = "owen", DisplayName = "Owen" });
        core.Accounts.Users.Add(new UserAccount { Username = "vera", DisplayName = "Vera" });
        var owen = core.Accounts.User("owen").Id;
        var space = await core.SpaceManager().CreateSpaceAsync(owen, new CreateSpaceRequest("Home", null), default);
        await core.SpaceManager().AddMemberAsync(owen, space.Id, new AddSpaceMemberRequest("vera", SpaceRole.Viewer), default);
        var types = core.TypeManager();
        var box = await types.CreateTypeAsync(owen, space.Id, new ItemTypeRequest("Box", null, null, true, [], 0), default);
        var tool = await types.CreateTypeAsync(owen, space.Id, new ItemTypeRequest("Tool", null, null, false,
        [
            new FieldDefinition(null, "brand", "Brand", FieldKind.Text, null, [], false, true),
            new FieldDefinition(null, "price", "Price", FieldKind.Money, "USD", [], false, true),
            new FieldDefinition(null, "power", "Power", FieldKind.Select, null, ["Hand", "Cordless"], false, false),
            new FieldDefinition(null, "bought", "Bought", FieldKind.Date, null, [], false, true),
        ], 0), default);
        return new ItemSpace(core, space.Id, box, tool);
    }

    /// <summary>Adds a box (a container) inside <paramref name="parent"/>, or at the top.</summary>
    public Task<ItemResponse> BoxAsync(string name, ItemResponse? parent = null) =>
        Items.CreateItemAsync(Owen, SpaceId, Request(name) with { TypeId = Box.Id, ParentId = parent?.Id }, default);

    /// <summary>Adds a thing with no type inside <paramref name="parent"/>, or in the Inbox.</summary>
    public Task<ItemResponse> ThingAsync(string name, ItemResponse? parent = null, decimal? quantity = null) =>
        Items.CreateItemAsync(Owen, SpaceId, Request(name) with { ParentId = parent?.Id, Quantity = quantity }, default);

    public static ItemRequest Request(string name) => new(name, null, null, null, null, null, null, null, null, null, 0);

    /// <summary>An item as it is now, as a request to change it.</summary>
    public static ItemRequest Edit(ItemResponse item) => new(
        item.Name, item.Marking, item.TypeId, null, item.HoldsItems, item.Quantity, item.Unit,
        item.Fields.ToDictionary(pair => pair.Key, pair => pair.Value), item.Tags, item.Notes, item.Version);

    public static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
}
