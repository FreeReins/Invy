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
