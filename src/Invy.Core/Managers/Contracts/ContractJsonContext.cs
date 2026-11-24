using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts;

/// <summary>
/// The JSON shapes of everything in <see cref="Messages"/>, made by the source generator at build time instead
/// of by reflection while running. The API reads and writes request and response bodies with it, and the message
/// bus stores commands and events with it. Records used inside the listed ones, and their enums, are included
/// automatically; add a line here for each new request, response, command or event that is used on its own.
/// Enums are written by name: Invy's own say so themselves, and this covers <see cref="DayOfWeek"/>.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
// AI models
[JsonSerializable(typeof(AiSettingsRequest))]
[JsonSerializable(typeof(AiSettingsResponse))]
[JsonSerializable(typeof(AiFeaturesResponse))]
[JsonSerializable(typeof(AiModelTestResult))]
// Accounts
[JsonSerializable(typeof(NewAccount))]
[JsonSerializable(typeof(SignInRequest))]
[JsonSerializable(typeof(AcceptInviteRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(SecurityRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(AccountSession))]
[JsonSerializable(typeof(IReadOnlyList<UserResponse>))]
[JsonSerializable(typeof(SessionResponse))]
[JsonSerializable(typeof(SecurityResponse))]
[JsonSerializable(typeof(IssuedLink))]
[JsonSerializable(typeof(IReadOnlyList<PendingLink>))]
[JsonSerializable(typeof(LinkDetails))]
[JsonSerializable(typeof(SecuritySettingsChanged))]
[JsonSerializable(typeof(CreateApiTokenRequest))]
[JsonSerializable(typeof(IssuedApiToken))]
[JsonSerializable(typeof(IReadOnlyList<ApiTokenResponse>))]
// The inbox
[JsonSerializable(typeof(InboxNoteRequest))]
[JsonSerializable(typeof(InboxResponse))]
[JsonSerializable(typeof(InboxChanged))]
// Spaces
[JsonSerializable(typeof(CreateSpaceRequest))]
[JsonSerializable(typeof(RenameSpaceRequest))]
[JsonSerializable(typeof(SpaceResponse))]
[JsonSerializable(typeof(IReadOnlyList<SpaceResponse>))]
[JsonSerializable(typeof(AddSpaceMemberRequest))]
[JsonSerializable(typeof(SpaceMemberRoleRequest))]
[JsonSerializable(typeof(IReadOnlyList<SpaceMemberResponse>))]
[JsonSerializable(typeof(IReadOnlyList<StarterPackResponse>))]
[JsonSerializable(typeof(IReadOnlyList<SpaceFeatureResponse>))]
[JsonSerializable(typeof(SpaceFeaturesRequest))]
[JsonSerializable(typeof(SpaceKindRequest))]
[JsonSerializable(typeof(GettingStartedResponse))]
[JsonSerializable(typeof(RemoveExamplesResponse))]
// Item types
[JsonSerializable(typeof(PackTypeRequest))]
[JsonSerializable(typeof(ItemTypeRequest))]
[JsonSerializable(typeof(ItemTypeResponse))]
[JsonSerializable(typeof(IReadOnlyList<ItemTypeResponse>))]
// Items
[JsonSerializable(typeof(ItemRequest))]
[JsonSerializable(typeof(ItemResponse))]
[JsonSerializable(typeof(ItemDetailResponse))]
[JsonSerializable(typeof(ItemPageResponse))]
[JsonSerializable(typeof(MoveItemRequest))]
[JsonSerializable(typeof(AdjustQuantityRequest))]
[JsonSerializable(typeof(QuickAddRequest))]
[JsonSerializable(typeof(PhotoRequest))]
[JsonSerializable(typeof(ItemSuggestionsResponse))]
[JsonSerializable(typeof(DetailSuggestionsResponse))]
[JsonSerializable(typeof(SuggestFieldsRequest))]
[JsonSerializable(typeof(FieldSuggestionsResponse))]
[JsonSerializable(typeof(IReadOnlyList<ItemResponse>))]
[JsonSerializable(typeof(ItemHistoryResponse))]
[JsonSerializable(typeof(ActivityResponse))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(AuditRequest))]
[JsonSerializable(typeof(AuditResponse))]
[JsonSerializable(typeof(WhereIsResponse))]
[JsonSerializable(typeof(ItemChangesRequest))]
[JsonSerializable(typeof(MoveItemsRequest))]
[JsonSerializable(typeof(ItemsChanged))]
[JsonSerializable(typeof(SuggestInboxTypes))]
[JsonSerializable(typeof(InboxTypesSuggested))]
// Attachments
[JsonSerializable(typeof(AddAttachmentRequest))]
[JsonSerializable(typeof(AttachmentResponse))]
[JsonSerializable(typeof(IReadOnlyList<AttachmentResponse>))]
[JsonSerializable(typeof(MakeThumbnail))]
[JsonSerializable(typeof(AttachFromWebRequest))]
[JsonSerializable(typeof(ProductResponse))]
// Labels
[JsonSerializable(typeof(CreateLabelsRequest))]
[JsonSerializable(typeof(IReadOnlyList<LabelResponse>))]
[JsonSerializable(typeof(LabelResponse))]
[JsonSerializable(typeof(LabelPageResponse))]
[JsonSerializable(typeof(ClaimLabelRequest))]
[JsonSerializable(typeof(AttachLabelRequest))]
// Maintenance
[JsonSerializable(typeof(NotificationSettingsRequest))]
[JsonSerializable(typeof(NotificationSettingsResponse))]
[JsonSerializable(typeof(DeliveryResult))]
[JsonSerializable(typeof(ReminderRun))]
[JsonSerializable(typeof(BackupSettingsRequest))]
[JsonSerializable(typeof(BackupsResponse))]
[JsonSerializable(typeof(BackupSummary))]
[JsonSerializable(typeof(SendDueReminders))]
[JsonSerializable(typeof(SendReminder))]
[JsonSerializable(typeof(RunDueBackup))]
public sealed partial class ContractJsonContext : JsonSerializerContext;
