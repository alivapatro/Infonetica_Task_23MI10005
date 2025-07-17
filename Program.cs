using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory stores
var workflowDefinitions = new ConcurrentDictionary<string, WorkflowDefinition>();
var workflowInstances = new ConcurrentDictionary<string, WorkflowInstance>();

// Hello World
app.MapGet("/", () => "Workflow Engine API");

// Create a new workflow definition
app.MapPost("/workflows", (WorkflowDefinition def) =>
{
    if (string.IsNullOrWhiteSpace(def.Id) || workflowDefinitions.ContainsKey(def.Id))
        return Results.BadRequest("Workflow definition must have a unique, non-empty Id.");

    if (def.States.Count == 0 || def.States.Count(s => s.IsInitial) != 1)
        return Results.BadRequest("Workflow must have exactly one initial state.");

    if (def.States.GroupBy(s => s.Id).Any(g => g.Count() > 1))
        return Results.BadRequest("Duplicate state IDs detected.");

    if (def.Actions.GroupBy(a => a.Id).Any(g => g.Count() > 1))
        return Results.BadRequest("Duplicate action IDs detected.");

    if (def.Actions.Any(a => a.ToState == null || !def.States.Any(s => s.Id == a.ToState)))
        return Results.BadRequest("Action 'toState' must exist in defined states.");

    workflowDefinitions[def.Id] = def;
    return Results.Ok(def);
});

// Get a workflow definition
app.MapGet("/workflows/{id}", (string id) =>
{
    if (workflowDefinitions.TryGetValue(id, out var def))
        return Results.Ok(def);
    return Results.NotFound("Workflow definition not found");
});

// Start a workflow instance
app.MapPost("/workflows/{id}/instances", (string id) =>
{
    if (!workflowDefinitions.TryGetValue(id, out var def))
        return Results.NotFound("Workflow definition not found");

    var initialState = def.States.First(s => s.IsInitial);

    var instance = new WorkflowInstance
    {
        Id = Guid.NewGuid().ToString(),
        DefinitionId = id,
        CurrentStateId = initialState.Id,
        History = new List<ActionHistory>()
    };

    workflowInstances[instance.Id] = instance;
    return Results.Ok(instance);
});

// Get a workflow instance
app.MapGet("/instances/{id}", (string id) =>
{
    if (workflowInstances.TryGetValue(id, out var instance))
        return Results.Ok(instance);
    return Results.NotFound("Workflow instance not found");
});

// Execute an action on an instance
app.MapPost("/instances/{instanceId}/actions/{actionId}", (string instanceId, string actionId) =>
{
    if (!workflowInstances.TryGetValue(instanceId, out var instance))
        return Results.NotFound("Instance not found");

    if (!workflowDefinitions.TryGetValue(instance.DefinitionId, out var def))
        return Results.NotFound("Definition not found");

    var action = def.Actions.FirstOrDefault(a => a.Id == actionId);
    if (action == null)
        return Results.BadRequest("Action not found");

    if (!action.Enabled)
        return Results.BadRequest("Action is disabled");

    if (!action.FromStates.Contains(instance.CurrentStateId))
        return Results.BadRequest("Current state does not allow this action");

    var currentState = def.States.First(s => s.Id == instance.CurrentStateId);
    if (currentState.IsFinal)
        return Results.BadRequest("Cannot act on a final state");

    var toState = def.States.FirstOrDefault(s => s.Id == action.ToState);
    if (toState == null)
        return Results.BadRequest("Target state not found");

    instance.CurrentStateId = toState.Id;
    instance.History.Add(new ActionHistory
    {
        ActionId = action.Id,
        Timestamp = DateTime.UtcNow
    });

    return Results.Ok(instance);
});

app.Run();

// Models
public class State
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public bool Enabled { get; set; } = true;
}

public class ActionTransition
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Enabled { get; set; } = true;
    public List<string> FromStates { get; set; } = new();
    public string ToState { get; set; } = default!;
}

public class WorkflowDefinition
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<State> States { get; set; } = new();
    public List<ActionTransition> Actions { get; set; } = new();
}

public class WorkflowInstance
{
    public string Id { get; set; } = default!;
    public string DefinitionId { get; set; } = default!;
    public string CurrentStateId { get; set; } = default!;
    public List<ActionHistory> History { get; set; } = new();
}

public class ActionHistory
{
    public string ActionId { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}
