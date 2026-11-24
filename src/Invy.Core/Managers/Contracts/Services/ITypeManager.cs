using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// The item types of a space and their custom fields. Any member can look; editors and owners change them.
/// Changing a type never rewrites items: values of a removed field are kept and shown as Other fields.
/// </summary>
public interface ITypeManager
{
    /// <summary>The space's types, by name.</summary>
    Task<IReadOnlyList<ItemTypeResponse>> ListTypesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    Task<ItemTypeResponse> GetTypeAsync(Guid userId, Guid spaceId, Guid typeId, CancellationToken cancellationToken);

    /// <summary>Adds a type to the space.</summary>
    /// <exception cref="RuleViolationException">A name, field or option breaks a rule.</exception>
    /// <exception cref="ConflictException">Another type has the name.</exception>
    Task<ItemTypeResponse> CreateTypeAsync(Guid userId, Guid spaceId, ItemTypeRequest request, CancellationToken cancellationToken);

    /// <summary>Copies one type, with its fields, from any starter pack into the space. Editors only.</summary>
    /// <exception cref="RuleViolationException">There is no such pack, or it has no such type.</exception>
    /// <exception cref="ConflictException">The space already has a type with the name.</exception>
    Task<ItemTypeResponse> CopyPackTypeAsync(Guid userId, Guid spaceId, PackTypeRequest request, CancellationToken cancellationToken);

    /// <summary>Replaces a type's name, look and fields.</summary>
    /// <exception cref="ConflictException">Someone changed it since <see cref="ItemTypeRequest.Version"/>, or another type has the name.</exception>
    Task<ItemTypeResponse> UpdateTypeAsync(
        Guid userId, Guid spaceId, Guid typeId, ItemTypeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Asks an AI model which fields a type called <see cref="SuggestFieldsRequest.Name"/> could have. Editors only.
    /// Nothing is saved: the person adds the ones they want and saves the type.
    /// </summary>
    /// <exception cref="RuleViolationException">No name, or no AI model is set up.</exception>
    /// <exception cref="AiUnavailableException">The model couldn't be reached or gave no answer Invy can read.</exception>
    Task<FieldSuggestionsResponse> SuggestFieldsAsync(Guid userId, Guid spaceId, SuggestFieldsRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a type. Items of that type keep their values.</summary>
    Task DeleteTypeAsync(Guid userId, Guid spaceId, Guid typeId, CancellationToken cancellationToken);
}
