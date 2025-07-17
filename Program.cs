using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Setup for Swagger for docs and testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory stores for workflow definitions and running instances
var definitionsStore = new ConcurrentDictionary<string, WorkflowDefinition>();
var instanceStore = new ConcurrentDictionary<string, WorkflowInstance>();

// welcome route
app.MapGet("/", () => "Welcome to the Workflow Engine API");

// Create a workflow definition
app.MapPost("/workflows", (WorkflowDefinition workflow) =>
{
    // Make sure ID is present and unique
    if (string.IsNullOrWhiteSpace(workflow.Id) || definitionsStore.ContainsKey(workflow.Id))
        return Results.BadRequest("Missing or duplicate workflow Id.");

    // Needs one initial state exactly
    var initialCount = workflow.States.Count(s => s.IsInitial);
    if (initialCount != 1)
        return Results.BadRequest("Workflow should have one initial state (found " + initialCount + ").");

    // Check for repeated state IDs
    if (workflow.States.GroupBy(s => s.Id).Any(g => g.Count() > 1))
        return Results.BadRequest("Duplicate state IDs found.");

    // Check for repeated action IDs too
    if (workflow.Actions.GroupBy(a => a.Id).Any(g => g.Count() > 1))
        return Results.BadRequest("Duplicate action IDs found.");

    // Every action's target state must actually exist
    foreach (var action in workflow.Actions)
    {
        if (string.IsNullOrWhiteSpace(action.ToState) || !workflow.States.Any(s => s.Id == action.ToState))
            return Results.BadRequest($"Invalid target state for action: {action.Id}");
    }

    definitionsStore[workflow.Id] = workflow;
    return Results.Ok(workflow);
});

// Get a workflow definition by ID
app.MapGet("/workflows/{id}", (string id) =>
{
    if (definitionsStore.TryGetValue(id, out var found))
        return Results.Ok(found);

    return Results.NotFound("Workflow not found, check the ID");
});

// Start a new workflow instance
app.MapPost("/workflows/{id}/instances", (string id) =>
{
    if (!definitionsStore.TryGetValue(id, out var definition))
        return Results.NotFound("No workflow found with given ID, check the ID.");

    // I’m assuming this always has one initial state due to earlier validation
    var startingState = definition.States.FirstOrDefault(s => s.IsInitial);
    if (startingState == null)
        return Results.BadRequest("Initial state is missing, check the workflow definition");

    var newInstance = new WorkflowInstance
    {
        Id = Guid.NewGuid().ToString(),
        DefinitionId = id,
        CurrentStateId = startingState.Id,
        History = new List<ActionHistory>()
    };

    instanceStore[newInstance.Id] = newInstance;
    return Results.Ok(newInstance);
});

// Retrieve a specific workflow instance
app.MapGet("/instances/{id}", (string id) =>
{
    if (instanceStore.TryGetValue(id, out var instance))
        return Results.Ok(instance);

    return Results.NotFound("Workflow instance not found, check the ID");
});

// Apply an action transition on an instance
app.MapPost("/instances/{instanceId}/actions/{actionId}", (string instanceId, string actionId) =>
{
    if (!instanceStore.TryGetValue(instanceId, out var runningInstance))
        return Results.NotFound("Workflow instance not found, check the ID");

    if (!definitionsStore.TryGetValue(runningInstance.DefinitionId, out var definition))
        return Results.NotFound("Definition for this instance is missing.");

    var actionToApply = definition.Actions.FirstOrDefault(a => a.Id == actionId);
    if (actionToApply == null)
        return Results.BadRequest("No such action in the workflow.");

    if (!actionToApply.Enabled)
        return Results.BadRequest("Action is currently disabled.");

    // checking if this action can be done from current state
    if (!actionToApply.FromStates.Contains(runningInstance.CurrentStateId))
        return Results.BadRequest("Action not valid from current state.");

    var currentState = definition.States.First(s => s.Id == runningInstance.CurrentStateId);
    if (currentState.IsFinal)
        return Results.BadRequest("Current state is final. Can't move forward.");

    // Double checking if the next state is valid
    var nextState = definition.States.FirstOrDefault(s => s.Id == actionToApply.ToState);
    if (nextState == null)
        return Results.BadRequest("Next state not found. Definition might be broken.");

    runningInstance.CurrentStateId = nextState.Id;
    runningInstance.History.Add(new ActionHistory
    {
        ActionId = actionToApply.Id,
        Timestamp = DateTime.UtcNow
    });

    return Results.Ok(runningInstance);
});

app.Run();




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
