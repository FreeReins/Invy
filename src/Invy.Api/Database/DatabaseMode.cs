namespace Invy.Api.Database;

/// <summary>Where Invy's PostgreSQL database comes from (the Database:Mode setting).</summary>
public enum DatabaseMode
{
    /// <summary>Invy runs its own PostgreSQL, from the copy shipped with it, keeping the data under Data:Root.</summary>
    Bundled,

    /// <summary>Invy connects to a PostgreSQL server someone else runs, given by ConnectionStrings:Invy.</summary>
    External,
}
