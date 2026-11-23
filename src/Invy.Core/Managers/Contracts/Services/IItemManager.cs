using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// A space's items: places, containers and things, nested to any depth, with each one's timeline. Every call takes the
/// asking person's account id and checks their role in the space first: viewers look, editors and owners change.
/// </summary>
public interface IItemManager
{
    /// <summary>An item with the chain of containers it is in.</summary>
    Task<ItemDetailResponse> GetItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// What is directly inside a container, or at the top when <paramref name="parentId"/> is null (the Inbox is left
    /// out there), by name. Disposed and archived items are left out. <paramref name="after"/> is the last page's Next.
    /// </summary>
    Task<ItemPageResponse> ListContentsAsync(Guid userId, Guid spaceId, Guid? parentId, string? after, CancellationToken cancellationToken);

    /// <summary>The Inbox: items with no place and no type, to tidy later. Paged like <see cref="ListContentsAsync"/>.</summary>
    Task<ItemPageResponse> ListInboxAsync(Guid userId, Guid spaceId, string? after, CancellationToken cancellationToken);

    /// <summary>Adds an item, in the container named, or at the top. Writes a Created event.</summary>
    /// <exception cref="RuleViolationException">A detail or field value breaks a rule, or the container doesn't hold items.</exception>
    Task<ItemResponse> CreateItemAsync(Guid userId, Guid spaceId, ItemRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes an item's details, type and field values. Writes a Changed event listing what changed, and a
    /// QuantityChanged event if a counted item's amount changed.
    /// </summary>
    /// <exception cref="ConflictException">Someone changed it since <see cref="ItemRequest.Version"/>.</exception>
    Task<ItemResponse> UpdateItemAsync(Guid userId, Guid spaceId, Guid id, ItemRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Puts an item in another container, or at the top. Everything inside it goes with it: one Moved event is
    /// written, for the item alone. Refused into the item itself or anything inside it.
    /// </summary>
    Task<ItemResponse> MoveItemAsync(Guid userId, Guid spaceId, Guid id, MoveItemRequest request, CancellationToken cancellationToken);

    /// <summary>Adds to or takes from a counted item's amount. Writes a QuantityChanged event.</summary>
    Task<ItemResponse> AdjustQuantityAsync(Guid userId, Guid spaceId, Guid id, AdjustQuantityRequest request, CancellationToken cancellationToken);

    /// <summary>Marks an item thrown away, sold or given away. Refused while anything is inside it. Writes a Disposed event.</summary>
    Task<ItemResponse> DisposeItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Brings a disposed or archived item back. Writes a Restored event.</summary>
    Task<ItemResponse> RestoreItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Adds several items typed in one line, such as "baseball, glove, 2 bats", in a container or the Inbox.</summary>
    Task<IReadOnlyList<ItemResponse>> QuickAddAsync(Guid userId, Guid spaceId, QuickAddRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an item seen where Invy says it is, and when it holds items, everything directly inside it too: opening or
    /// scanning a container does this. Writes one Seen event, on the item alone. Marking it again within
    /// <see cref="SeenAgainAfter"/> changes nothing.
    /// </summary>
    Task<ItemResponse> MarkSeenAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Finishes an audit walk of a container: what was ticked is seen (and found, if it was missing); what is directly
    /// inside but wasn't ticked is marked missing, with a Missing event each. Lent items are left alone.
    /// </summary>
    /// <exception cref="RuleViolationException">It doesn't hold items, or holds more than an audit can check at once.</exception>
    Task<AuditResponse> AuditAsync(Guid userId, Guid spaceId, Guid id, AuditRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Looks a UPC or EAN barcode up online, to fill in a new item's name, brand and photo. Editors only, and only once
    /// an admin has turned barcode lookup on.
    /// </summary>
    /// <exception cref="RuleViolationException">Lookup is off, or the text isn't a product barcode.</exception>
    /// <exception cref="NotFoundException">No database knows it.</exception>
    /// <exception cref="WebSourceException">The databases couldn't be reached.</exception>
    Task<ProductResponse> LookUpBarcodeAsync(Guid userId, Guid spaceId, string barcode, CancellationToken cancellationToken);

    /// <summary>How long after being marked seen an item is marked again, so opening a box twice writes one event.</summary>
    static TimeSpan SeenAgainAfter => TimeSpan.FromMinutes(10);

    /// <summary>An item's timeline and where it has been, counting its containers' moves.</summary>
    Task<ItemHistoryResponse> GetHistoryAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds items by what people type (docs/developers/design.md, Search), 50 at a time. <paramref name="sort"/> is
    /// relevance (the default with words), name (the default without) or recent. Disposed and archived items are left
    /// out unless asked for with is:disposed or is:archived.
    /// </summary>
    /// <exception cref="RuleViolationException">A term can't be read; the message says how to write it.</exception>
    Task<SearchResponse> SearchAsync(Guid userId, Guid spaceId, string? text, string? sort, string? after, CancellationToken cancellationToken);

    /// <summary>Recounts the contents and value totals of the containers an <see cref="ItemsChanged"/> names. Safe to run twice.</summary>
    Task UpdateRollupsAsync(ItemsChanged message, CancellationToken cancellationToken);

    /// <summary>The space's timeline, newest first. <paramref name="before"/> is the last page's Next.</summary>
    Task<ActivityResponse> ListActivityAsync(Guid userId, Guid spaceId, string? before, CancellationToken cancellationToken);
}
