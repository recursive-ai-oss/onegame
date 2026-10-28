[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioRunner class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public sealed class ScenarioRunner
```

Runs scenarios against a game through the control layer, the same operations an agent calls over MCP.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioRunner(ControlSurface, ScenarioRunOptions?)`](#m-ctor-controlsurface-scenariorunoptions) | Creates a runner for a control surface. |

### <a id="m-ctor-controlsurface-scenariorunoptions"></a>ScenarioRunner(ControlSurface, ScenarioRunOptions?)

```csharp
public ScenarioRunner(ControlSurface surface, ScenarioRunOptions? options = null);
```

Creates a runner for a control surface.

**Parameters**

- `surface` ([`ControlSurface`](../../OneGame.Control/OneGame.Control/ControlSurface.md))
- `options` ([`ScenarioRunOptions`](ScenarioRunOptions.md)?)

## Methods

| Member | Description |
| --- | --- |
| [`Run(Scenario)`](#m-run-scenario) | Runs one scenario. |
| [`RunAsync(Scenario)`](#m-runasync-scenario) | Runs one scenario like [`ScenarioRunner.Run()`](ScenarioRunner.md#m-run-scenario). |

### <a id="m-run-scenario"></a>Run(Scenario)

```csharp
public ScenarioResult Run(Scenario scenario);
```

Runs one scenario. Steps stop at the first failure; expectations are checked only when every step ran.

**Parameters**

- `scenario` ([`Scenario`](Scenario.md))

**Returns:** [`ScenarioResult`](ScenarioResult.md)

### <a id="m-runasync-scenario"></a>RunAsync(Scenario)

```csharp
public ValueTask<ScenarioResult> RunAsync(Scenario scenario);
```

Runs one scenario like [`ScenarioRunner.Run()`](ScenarioRunner.md#m-run-scenario). Captures are the only steps that can take a while to finish (a browser reads pixels back asynchronously), so this is the form a browser page uses; on other platforms it completes before returning.

**Parameters**

- `scenario` ([`Scenario`](Scenario.md))

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`ScenarioResult`](ScenarioResult.md)&gt;
