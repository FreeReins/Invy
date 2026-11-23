using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What kind of value a custom field on an item type holds.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FieldKind>))]
public enum FieldKind
{
    /// <summary>One line of text.</summary>
    Text,

    /// <summary>Several lines of text.</summary>
    LongText,

    /// <summary>A number, with an optional unit.</summary>
    Number,

    /// <summary>An amount of money, with an optional currency as its unit.</summary>
    Money,

    /// <summary>A day.</summary>
    Date,

    /// <summary>Yes or no.</summary>
    Bool,

    /// <summary>One of the field's options.</summary>
    Select,

    /// <summary>Any number of the field's options.</summary>
    MultiSelect,

    /// <summary>A web address.</summary>
    Url,

    /// <summary>A barcode number, such as an EAN or UPC.</summary>
    Barcode,

    /// <summary>A person's name.</summary>
    Person,

    /// <summary>Another item in the same space.</summary>
    Item,
}
