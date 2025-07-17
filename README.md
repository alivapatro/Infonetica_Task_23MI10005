Sure! Here’s a more concise and clean version of your `README.md` with all essential info:

# WorkflowEngine

A lightweight .NET 8 backend for creating and managing state-machine workflows — fully in-memory and built with Minimal API.

## 🚀 Getting Started

```bash
dotnet run
```

Access Swagger UI at: [http://localhost:5268/swagger](http://localhost:5268/swagger)

## API Overview

### Workflow Definition

* `POST /workflows` – Create a workflow (states + actions)
* `GET /workflows` – List all workflows
* `GET /workflows/{id}` – Get workflow by ID

### Workflow Runtime

* `POST /instances?definitionId=...` – Start an instance
* `GET /instances` – List all instances
* `GET /instances/{id}` – Get instance by ID
* `POST /instances/{id}/actions?actionId=...` – Execute an action

---

## 📦 Sample Payload

```json
{
  "id": "leave-approval",
  "name": "Leave Approval",
  "states": [
    { "id": "draft", "name": "Draft", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "approved", "name": "Approved", "isFinal": true, "enabled": true },
    { "id": "rejected", "name": "Rejected", "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "approve", "name": "Approve", "fromStates": ["draft"], "toState": "approved", "enabled": true },
    { "id": "reject", "name": "Reject", "fromStates": ["draft"], "toState": "rejected", "enabled": true }
  ]
}
```

✅ Notes

* In-memory only (no DB)
* Validates uniqueness, transitions, and final states
* All logic in `Program.cs` for simplicity


