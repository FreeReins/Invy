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

    /// <summary>
    /// Moves several items into one container (or the top), in order, as <see cref="MoveItemAsync"/> does for each.
    /// Each move is saved on its own, so when one is refused the ones before it stay moved.
    /// </summary>
    Task<IReadOnlyList<ItemResponse>> MoveItemsAsync(Guid userId, Guid spaceId, MoveItemsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes only what the request names, as <see cref="UpdateItemAsync"/> would with the rest left as it is. Field
    /// values are given as text by search name and read as their field's kind.
    /// </summary>
    /// <exception cref="RuleViolationException">A field name isn't one of its type's, or a value can't be read as its kind.</exception>
    Task<ItemResponse> ChangeItemAsync(Guid userId, Guid spaceId, Guid id, ItemChangesRequest request, CancellationToken cancellationToken);

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

    /// <summary>
    /// Asks an AI model what is in photos of an open box, matched to the space's types and fields. Editors only. The
    /// answer is only suggestions: nothing is added until the person adds the ones they keep.
    /// </summary>
    /// <exception cref="RuleViolationException">No photo, a file that isn't one, or no AI model is set up.</exception>
    /// <exception cref="AiUnavailableException">The model couldn't be reached or gave no answer Invy can read.</exception>
    Task<ItemSuggestionsResponse> SuggestFromPhotoAsync(Guid userId, Guid spaceId, PhotoRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Asks an AI model to read an item's fields (brand, model, serial) from photos of a label or nameplate. Editors
    /// only. Nothing is saved: the person keeps the values they want and saves the item.
    /// </summary>
    /// <exception cref="RuleViolationException">The item's type has no fields, no photo, or no AI model is set up.</exception>
    /// <exception cref="AiUnavailableException">The model couldn't be reached or gave no answer Invy can read.</exception>
    Task<DetailSuggestionsResponse> SuggestDetailsFromPhotoAsync(Guid userId, Guid spaceId, Guid id, PhotoRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Asks for a type for every Inbox item without a suggestion, again for ones asked about before, on the ai queue.
    /// Suggestions appear in the Inbox as they come. Editors only.
    /// </summary>
    /// <exception cref="RuleViolationException">No AI model is set up, or the space has no types.</exception>
    Task SuggestInboxTypesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>
    /// Asks an AI model for the types of a batch of Inbox items and keeps each answer as a suggestion; queues the next
    /// batch when there may be more. Does nothing without a model or types. Safe to run twice.
    /// </summary>
    Task SweepInboxTypesAsync(SuggestInboxTypes message, CancellationToken cancellationToken);

    /// <summary>Gives an Inbox item the type suggested for it, as changing its type on its page would (a Changed event).</summary>
    /// <exception cref="RuleViolationException">It has no suggestion, or the suggested type was removed.</exception>
    Task<ItemResponse> AcceptSuggestedTypeAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Drops the type suggested for an Inbox item. It isn't suggested again unless someone asks for suggestions again.</summary>
    Task<ItemResponse> IgnoreSuggestedTypeAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

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

    /// <summary>
    /// Where the things matching <paramref name="text"/> are: a search (with the same syntax) whose best 10 matches
    /// come back with their containers as text. Any member can ask.
    /// </summary>
    Task<WhereIsResponse> WhereIsAsync(Guid userId, Guid spaceId, string text, CancellationToken cancellationToken);

    /// <summary>Recounts the contents and value totals of the containers an <see cref="ItemsChanged"/> names. Safe to run twice.</summary>
    Task UpdateRollupsAsync(ItemsChanged message, CancellationToken cancellationToken);

    /// <summary>The space's timeline, newest first. <paramref name="before"/> is the last page's Next.</summary>
    Task<ActivityResponse> ListActivityAsync(Guid userId, Guid spaceId, string? before, CancellationToken cancellationToken);
}
