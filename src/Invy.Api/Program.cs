using Invy.Api;
using Invy.Api.Database;
using Invy.Api.Mcp;
using Invy.Core;
using Invy.Core.Managers.Contracts;
using JasperFx;
using Microsoft.Extensions.Hosting.WindowsServices;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = WebPage.Folder(),
    // A Windows service starts in System32, so its settings and page are found from the app's own folder.
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});

// Everything Invy writes lives under Data:Root, which can hold a settings.json of its own.
DataFolders.UseDataRoot(builder.Configuration);
// Invy runs its own PostgreSQL unless Database:Mode is External. This sets the connection string, so it comes
// before anything reads it.
var database = BundledDatabase.Configure(builder.Configuration);
if (database is not null)
{
    builder.Services.AddSingleton(database);
    builder.Services.AddHostedService<BundledDatabaseLifetime>();
}

// Creates Invy's tables as the app starts, after the database and before the message bus, which creates its own.
// Registered before UseWolverine, so it runs first.
builder.Services.AddHostedService<InvyStartup>();

// Runs as the Invy Windows service when the installer set it up as one; otherwise this does nothing.
builder.Host.UseWindowsService(service => service.ServiceName = "Invy");
if (OperatingSystem.IsWindows())
{
    builder.Services.Configure<Microsoft.Extensions.Logging.EventLog.EventLogSettings>(WindowsEventLog.UseInvySource);
}

builder.Services.AddInvy(builder.Configuration);
builder.Services.AddInvySignIn(builder.Configuration);
builder.Logging.AddInvyLogViewer();

// HTTP always; HTTPS too when Https:Mode is SelfSigned or Certificate.
var certificates = builder.Services.AddInvyHttps(builder.Configuration);
builder.WebHost.ConfigureKestrel(certificates.Configure);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<InvyExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddInvyWebPage();
// Request and response bodies use JSON made at build time (each enum says it is written as text).
builder.Services.ConfigureHttpJsonOptions(json =>
{
    json.SerializerOptions.TypeInfoResolverChain.Insert(0, ContractJsonContext.Default);
    json.SerializerOptions.TypeInfoResolverChain.Insert(1, ApiJsonContext.Default);
});

// Wolverine runs the HTTP endpoints (Endpoints/) and the message handlers (Messaging/). Messages the managers
// publish are kept in PostgreSQL with their data, so they survive a restart.
builder.Host.UseWolverine(wolverine =>
{
    wolverine.UseInvyMessageStorage(builder.Configuration);
    wolverine.ResolveInvyServicesFromContainer();
});
builder.Services.UseInvyGeneratedCode();
builder.Services.AddWolverineHttp();

// The MCP server at /mcp, for assistants signed in with an API token (Settings, AI assistants). Its tools call the
// managers just as the endpoints do (the add-mcp-tool skill). McpCaller reads who is calling and which space.
builder.Services.AddMcpServer(mcp => mcp.ServerInfo = new() { Name = "Invy", Version = "1.0" })
    .WithHttpTransport(mcp => mcp.Stateless = true)
    .WithTools<SpaceTools>(McpJsonContext.ToolOptions)
    .WithTools<ItemTools>(McpJsonContext.ToolOptions)
    .WithTools<TypeTools>(McpJsonContext.ToolOptions)
    .WithTools<ViewTools>(McpJsonContext.ToolOptions);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<McpCaller>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseInvySecurityHeaders();
app.UseInvyProxyHeaders();
app.UseInvyHttpsRedirect();
app.UseInvyWebPage();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi().RequireAuthorization();
// A Swagger page at /docs for trying the API. The page itself is open, but it reads /openapi/v1.json with the
// browser's sign-in cookie, so it only lists the API once you are signed in.
app.UseSwaggerUI(swagger =>
{
    swagger.RoutePrefix = "docs";
    swagger.DocumentTitle = "Invy API";
    swagger.SwaggerEndpoint("/openapi/v1.json", "Invy API");
});
app.MapGet("/health", () => Results.Ok(new HealthResponse("ok"))).ExcludeFromDescription();

// Every API endpoint needs a signed-in user unless it is marked [AllowAnonymous].
app.MapWolverineEndpoints(endpoints => endpoints.RequireAuthorizeOnAll());
app.MapMcp("/mcp").RequireAuthorization(Sessions.McpPolicy);

app.MapInvyWebPage();

// `dotnet run -- codegen write` writes the code Wolverine runs in Release builds; see CONTRIBUTING.md. It stops here,
// before anything touches the database.
if (args is ["codegen", ..])
{
    return await app.RunJasperFxCommands(args);
}

try
{
    // Starts the bundled database and creates the tables first (BundledDatabaseLifetime, InvyStartup).
    await app.StartAsync();
    await app.Services.ApplyInvyStartupSettingsAsync();
    PortableApp.Announce(app.Configuration, app.Logger);
    await app.WaitForShutdownAsync();
    return 0;
}
finally
{
    // Normally stopped with the app already; this covers a start that failed part way.
    if (database is not null)
    {
        await database.StopAsync(app.Services.GetRequiredService<ILogger<BundledDatabase>>());
    }
}
