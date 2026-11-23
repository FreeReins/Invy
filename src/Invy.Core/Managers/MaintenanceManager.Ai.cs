using Invy.Core.Engines;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>The AI models Invy may use, and which does what.</content>
internal sealed partial class MaintenanceManager
{
    public async Task<AiSettingsResponse> GetAiSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetAiAsync(cancellationToken)).ToResponse();

    public async Task<AiSettingsResponse> SaveAiSettingsAsync(AiSettingsRequest request, CancellationToken cancellationToken)
    {
        var current = await settings.GetAiAsync(cancellationToken);
        var fresh = ValidationEngine.Ai(request, current).ToAiSettings(current);

        await settings.SaveAiAsync(fresh, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return fresh.ToResponse();
    }

    public async Task<AiFeaturesResponse> GetAiFeaturesAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetAiAsync(cancellationToken);
        return new AiFeaturesResponse(
            Enum.GetValues<AiTask>().Where(task => ValidationEngine.AiModelOrNull(task, current) is not null).ToList());
    }

    public async Task<AiModelTestResult> TestAiModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = ValidationEngine.AiModel(await settings.GetAiAsync(cancellationToken), id);
        try
        {
            await ai.AskAsync(model, TransformationEngine.AiTestQuestion(), cancellationToken);
            return new AiModelTestResult(true);
        }
        catch (AiUnavailableException exception)
        {
            return new AiModelTestResult(false, exception.Message);
        }
    }
}
