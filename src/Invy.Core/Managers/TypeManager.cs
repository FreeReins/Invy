using Invy.Core.Engines;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="ITypeManager"/>
internal sealed class TypeManager(
    ITypeAccess types, ISettingsAccess settings, IAiAccess ai, IMessageAccess messages, ValidationEngine validation) : ITypeManager
{
    public async Task<IReadOnlyList<ItemTypeResponse>> ListTypesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return (await types.ListAsync(spaceId, cancellationToken)).Select(type => type.ToResponse()).ToList();
    }

    public async Task<ItemTypeResponse> GetTypeAsync(Guid userId, Guid spaceId, Guid typeId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return (await validation.TypeAsync(spaceId, typeId, cancellationToken)).ToResponse();
    }

    public async Task<ItemTypeResponse> CreateTypeAsync(Guid userId, Guid spaceId, ItemTypeRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var type = ValidationEngine.TypeDraft(spaceId, Guid.CreateVersion7(), request);
        type.Version = 1;
        await validation.UniqueTypeNameAsync(spaceId, type.Name, type.Id, cancellationToken);
        await types.SaveAsync(type, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return type.ToResponse();
    }

    public async Task<ItemTypeResponse> UpdateTypeAsync(
        Guid userId, Guid spaceId, Guid typeId, ItemTypeRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var current = await validation.TypeAsync(spaceId, typeId, cancellationToken);
        if (request.Version != current.Version)
        {
            throw new ConflictException($"Someone else changed {current.Name} while you were editing it. Reload to see their change, then try again.");
        }

        var type = ValidationEngine.TypeDraft(spaceId, typeId, request);
        type.Version = current.Version + 1;
        await validation.UniqueTypeNameAsync(spaceId, type.Name, type.Id, cancellationToken);
        await types.SaveAsync(type, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return type.ToResponse();
    }

    public async Task DeleteTypeAsync(Guid userId, Guid spaceId, Guid typeId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        await validation.TypeAsync(spaceId, typeId, cancellationToken);
        await types.RemoveAsync(spaceId, typeId, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<FieldSuggestionsResponse> SuggestFieldsAsync(
        Guid userId, Guid spaceId, SuggestFieldsRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > ValidationEngine.TypeNameLength)
        {
            throw new RuleViolationException("Name the type first, such as Lego sets, then ask for fields.");
        }

        var existing = (request.ExistingKeys ?? []).Select(key => key.Trim().ToLowerInvariant()).Where(key => key.Length > 0).Distinct().ToList();
        var model = ValidationEngine.AiModelFor(AiTask.SuggestTypeFields, await settings.GetAiAsync(cancellationToken));
        var answer = await ai.AskAsync(model, TransformationEngine.TypeFieldsQuestion(name, existing), cancellationToken);
        var fields = TransformationEngine.ReadTypeFields(answer, existing.Concat(ValidationEngine.ReservedFieldKeys))
            .Where(ValidationEngine.FieldFits)
            .ToList();
        return new FieldSuggestionsResponse(fields, model.Name);
    }
}
