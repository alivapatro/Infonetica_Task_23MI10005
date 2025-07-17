# WorkflowEngine

A minimal backend service for defining and running configurable state-machine workflows.

## Quick Start

1. **Run the API:**
   ```sh
   dotnet run
   ```
   The API will be available at `http://localhost:5268` (or as shown in your console).

2. **API Overview:**

### Workflow Definition
- `POST /workflows` — Create a new workflow definition (provide states and actions in body)
- `GET /workflows` — List all workflow definitions
- `GET /workflows/{id}` — Get a workflow definition by ID

### Workflow Runtime
- `POST /instances?definitionId=...` — Start a new workflow instance from a definition
- `GET /instances` — List all workflow instances
- `GET /instances/{id}` — Get a workflow instance by ID
- `POST /instances/{id}/actions?actionId=...` — Execute an action on a workflow instance

## Design Assumptions & Notes
- **In-memory only:** No database; all data is lost on restart.
- **Validation:**
  - Workflow definitions must have unique IDs, exactly one initial state, and no duplicate state/action IDs.
  - Action execution is validated for enabled status, valid source/target states, and not allowed from final states.
- **Extensibility:** Models and endpoints are designed for easy extension (e.g., add descriptions, more validation, persistence).
- **Minimal API:** All logic is in `Program.cs` for simplicity.

## Example Payloads

### Create Workflow Definition
```json
{
  "id": "leave-approval",
  "name": "Leave Approval Workflow",
  "states": [
    { "id": "draft", "name": "Draft", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "approved", "name": "Approved", "isInitial": false, "isFinal": true, "enabled": true },
    { "id": "rejected", "name": "Rejected", "isInitial": false, "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "approve", "name": "Approve", "enabled": true, "fromStates": ["draft"], "toState": "approved" },
    { "id": "reject", "name": "Reject", "enabled": true, "fromStates": ["draft"], "toState": "rejected" }
  ]
}
```

### Start Workflow Instance
`POST /instances?definitionId=leave-approval`

### Execute Action
`POST /instances/{instanceId}/actions?actionId=approve`

## Limitations
- No persistence or authentication.
- No partial updates to definitions (must provide full definition).
- Not production-ready; for demonstration and evaluation only. 