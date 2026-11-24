using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>Shared pieces for the AI task tests: a model to set up, and a photo's first bytes.</summary>
internal static class AiTestSetup
{
    /// <summary>Enough of a JPEG for Invy to tell it is one.</summary>
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F'];

    public static PhotoRequest Photo => new([Jpeg]);

    /// <summary>Sets up one model, Claude, as the default for every task.</summary>
    public static Task UseClaudeAsync(TestCore core) =>
        core.MaintenanceManager().SaveAiSettingsAsync(
            new([new AiModelDraft(null, "Claude", AiProvider.Anthropic, "claude-opus-5-5", ApiKey: "sk-ant-test")], "Claude"), default);
}
