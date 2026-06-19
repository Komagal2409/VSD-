# VSD Commissioning Suite — Simulation System

A full-stack simulation of a Variable Speed Drive (VSD) commissioning workflow.
- **Backend**: ASP.NET Core 8 Web API (no real hardware)
- **Frontend**: Windows Forms desktop application (.NET 8)

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| VS Code | Latest | https://code.visualstudio.com |
| C# Dev Kit (VS Code extension) | Latest | marketplace |

> **Windows only** — the Desktop app (WinForms) requires Windows.  
> The API runs on any OS.

---

## Project Structure

```
VsdCommissioningSuite/
├── VsdCommissioningSuite.sln          # Solution file
├── VsdApi/                            # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── CommissioningController.cs # All 6 endpoints
│   ├── Services/
│   │   └── SimulationEngine.cs        # Workflow simulation & error gen
│   ├── Models/
│   │   └── Models.cs                  # Shared DTOs
│   └── Program.cs
├── VsdDesktop/                        # Windows Forms App
│   ├── Models/Models.cs               # Mirror of API models
│   ├── Services/ApiClient.cs          # HttpClient wrapper
│   ├── MainForm.cs                    # Full dark-theme UI
│   └── Program.cs
└── .vscode/
    ├── launch.json                    # Debug configs
    └── tasks.json                     # Build tasks
```

---

## How to Run

### Option A — Two separate terminals (recommended)

**Terminal 1 — Start the API:**
```bash
cd VsdApi
dotnet run
```
API starts at: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

**Terminal 2 — Start the Desktop App:**
```bash
cd VsdDesktop
dotnet run
```

### Option B — VS Code debugger

1. Open `VsdCommissioningSuite.code-workspace` in VS Code
2. Press `F5` or go to Run → Start Debugging
3. Select **"VsdApi (ASP.NET Core)"** first, wait for it to start
4. Then launch **"VsdDesktop (WinForms)"**

---

## API Endpoints

| Method | Endpoint              | Description                          |
|--------|-----------------------|--------------------------------------|
| POST   | `/initialize`         | Step 1: Initialize VSD hardware bus  |
| POST   | `/validate-config`    | Step 2: Validate configuration       |
| POST   | `/upload-parameters`  | Step 3: Upload motor parameters      |
| POST   | `/motor-test`         | Step 4: Run motor test sequence      |
| POST   | `/commission`         | Step 5: Final commissioning          |
| GET    | `/logs`               | Get all session logs                 |
| GET    | `/logs?level=ERROR`   | Filter logs by level                 |
| DELETE | `/logs`               | Clear session logs                   |
| GET    | `/health`             | API health check                     |

---

## Simulated Behavior

Each endpoint randomly returns one of three outcomes:
- **Success** (55% probability) — step completes cleanly
- **Warning** (20% probability) — step completes with configuration warnings
- **Failure** (25% probability) — step fails with application or config errors

### Error Categories
| Category | Examples |
|---|---|
| `ApplicationError` | Bus timeout, watchdog trip, encoder fault, EEPROM write fail |
| `ConfigurationError` | Missing parameter, value out of range, checksum mismatch |

### Log Levels
All responses embed logs at four levels: `INFO`, `DEBUG`, `WARN`, `ERROR`

---

## Desktop App Features

- **Dark industrial theme** matching VSD/SCADA aesthetics
- **Sidebar navigation** for each workflow step
- **Per-step panels** with Run button, live status, response data, errors, embedded logs
- **System Logs panel** — all session logs with level filters
- **API Health panel** — connectivity check with live feedback
- **Status bar** — shows last action result

---

## Sample API Response

```json
{
  "success": true,
  "outcome": "Success",
  "step": "initialize",
  "message": "Drive initialized successfully.",
  "durationMs": 743,
  "data": {
    "HardwareId": "VSD-SIM-4821",
    "FirmwareVersion": "4.2.1",
    "BusAddress": "0x1A",
    "MemoryKB": 512
  },
  "errors": [],
  "logs": [
    { "timestamp": "14:32:01.421", "level": "INFO",  "source": "InitController", "message": "Drive initialization sequence started" },
    { "timestamp": "14:32:01.438", "level": "DEBUG", "source": "BusScanner",     "message": "Scanning CAN hardware bus..." },
    { "timestamp": "14:32:01.502", "level": "INFO",  "source": "BusScanner",     "message": "CAN interface detected at 0x1A" },
    { "timestamp": "14:32:01.611", "level": "INFO",  "source": "InitController", "message": "Firmware version validated: 4.2.1" }
  ]
}
```

---

## Troubleshooting

| Problem | Solution |
|---|---|
| Desktop shows "API Offline" | Run `dotnet run` in the VsdApi folder first |
| Port 5000 already in use | Change URL in `Program.cs` and `ApiClient.cs` |
| WinForms won't build on Linux/macOS | Expected — use WSL or build only the API |
| `dotnet` not found | Install .NET 8 SDK from microsoft.com/dotnet |
