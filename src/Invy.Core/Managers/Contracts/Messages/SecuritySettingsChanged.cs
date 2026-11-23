using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Event: the sign-in rules changed, including whether HTTPS is required.</summary>
[MessageIdentity("invy.security-settings-changed.v1")]
public sealed record SecuritySettingsChanged(bool RequireHttps);
