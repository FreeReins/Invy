using Invy.Core.Managers.Contracts;
using ModelContextProtocol;

namespace Invy.Api.Mcp;

/// <summary>
/// Passes Invy's own errors (no such item, a rule broken, ...) to the assistant with their plain message, so it can
/// say what went wrong. The MCP SDK only sends the message of an <see cref="McpException"/>; anything else becomes a
/// generic error.
/// </summary>
public static class McpErrors
{
    public static async Task<T> Explain<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (InvyException error)
        {
            throw new McpException(error.Message, error);
        }
    }

    public static async Task<string> Explain(Func<Task> call, string done)
    {
        try
        {
            await call();
            return done;
        }
        catch (InvyException error)
        {
            throw new McpException(error.Message, error);
        }
    }
}
