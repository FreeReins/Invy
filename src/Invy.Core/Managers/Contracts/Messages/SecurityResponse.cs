namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The sign-in rules, and what Invy may look up on the internet, as saved.</summary>
/// <param name="MaxFailedSignIns">Wrong passwords in a row before an account is locked. Zero turns lockout off.</param>
/// <param name="LockoutMinutes">How long a locked account stays locked.</param>
/// <param name="RequireHttps">Send plain HTTP visitors to HTTPS.</param>
/// <param name="BarcodeLookup">Look product barcodes up online.</param>
public sealed record SecurityResponse(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps, bool BarcodeLookup);
