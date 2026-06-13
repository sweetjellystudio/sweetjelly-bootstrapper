# SWeetJelly Initialiser

`SWeetJelly Initialiser` is a Unity bootstrap package for installing project modules and sharing the services they create.

It provides a small service registry, ScriptableObject-based init modules, and a scene `MonoBehaviour` that installs those modules during Unity startup.

## Purpose

Use this package when a Unity project needs one predictable place to compose shared systems such as Timer, Serialization, Save, Loading, Pooling, and project-specific services.

The Initialiser package is responsible for:

- creating a shared `ServiceRegistry`
- passing an `InitModuleInstallContext` to each init module
- installing modules in a stable order
- exposing registered services through `InitialiserBehaviour.Services`
- keeping module setup logic out of gameplay/domain classes

It is not intended to be a global service locator for arbitrary runtime code. Prefer resolving services in a composition root or bootstrapper, then passing dependencies explicitly.

## Package Structure

```text
Runtime/
  Contract/
    IServiceResolver.cs
    IServiceRegistry.cs
    IInitModule.cs
    RegisterModuleAttribute.cs

  App/
    ServiceRegistry.cs

  Framework/
    InitialiserBehaviour.cs
    InitModuleSo.cs
    InitModuleInstallContext.cs
    ProjectInitSettingSo.cs

Editor/
  Framework/
    InitModuleEditor.cs
```

## Assemblies

### `SWeetJelly.Initialiser.Contract`

Defines the public contracts used by modules and project bootstrap code.

- `IServiceResolver`
- `IServiceRegistry`
- `IInitModule`
- `RegisterModuleAttribute`

### `SWeetJelly.Initialiser.App`

Contains the Unity-independent registry implementation.

- `ServiceRegistry`

### `SWeetJelly.Initialiser.Framework`

Contains Unity-facing bootstrap types.

- `InitialiserBehaviour`
- `InitModuleSo`
- `InitModuleInstallContext`
- `ProjectInitSettingSo`

## Core Types

### `ServiceRegistry`

`ServiceRegistry` stores services by type.

```csharp
var registry = new ServiceRegistry();

registry.Register<IMyService>(new MyService());

var service = registry.Resolve<IMyService>();
```

Registering the same service type twice throws an exception. Resolving an unregistered service also throws.

Use `TryResolve<T>()` when a service is optional.

```csharp
if (registry.TryResolve<IMyService>(out var service))
{
    service.Run();
}
```

### `InitModuleSo`

`InitModuleSo` is the base class for modules that install services.

```csharp
public sealed class MyInitModule : InitModuleSo
{
    public override string ModuleName => "My Module";

    public override void Install(InitModuleInstallContext context)
    {
        context.Services.Register<IMyService>(new MyService());
    }

    public override void Uninstall(InitModuleInstallContext context)
    {
        context.Services.Unregister<IMyService>();
    }
}
```

Modules can be ordered with the serialized `InstallOrder` value on `InitModuleSo`. `InitialiserBehaviour` installs modules by `InstallOrder`, then by `ModuleName`.

### `InitModuleInstallContext`

The install context gives each module access to the shared scene/bootstrap objects.

```csharp
public sealed class InitModuleInstallContext
{
    public GameObject HolderObject { get; }
    public Canvas Canvas { get; }
    public EventSystem EventSystem { get; }
    public IServiceRegistry Services { get; }
}
```

Typical usage:

- attach framework behaviours to `HolderObject`
- use `Canvas` for UI-facing modules
- use `EventSystem` for input/UI modules
- register and resolve shared services through `Services`

### `ProjectInitSettingSo`

`ProjectInitSettingSo` is a ScriptableObject asset that stores the list of init modules to install.

Create it from:

```text
Create > SWeetJelly > Settings > Project Init Setting
```

Then assign module assets to its `Modules` list.

### `InitialiserBehaviour`

`InitialiserBehaviour` is the scene entry point.

On `Awake()` it:

1. creates a shared `ServiceRegistry`
2. prepares the `EventSystem` if needed
3. creates an `InitModuleInstallContext`
4. installs modules from `ProjectInitSettingSo`
5. exposes static accessors such as `IsInitialised` and `Services`

On destroy, it uninstalls installed modules in reverse order.

## Basic Setup

1. Create a `ProjectInitSettingSo` asset.
2. Create module assets such as Timer, Serialization, Save, and Loading init modules.
3. Add those module assets to the project init setting.
4. Add `InitialiserBehaviour` to an init scene object.
5. Assign the init setting, canvas, and optional event system.

Example scene object:

```text
Initialiser
  InitialiserBehaviour
    Init Setting: ProjectInitSetting
    Canvas: RootCanvas
    Event System: EventSystem
```

## Resolving Services

Project bootstrap code can resolve installed services after the initialiser has run.

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Timer.Contract;

public sealed class GameBootstrapBehaviour : MonoBehaviour
{
    private void Start()
    {
        if (!InitialiserBehaviour.IsInitialised)
            throw new InvalidOperationException("Initialiser is not initialised.");

        var timerService = InitialiserBehaviour.Services.Resolve<ITimerService>();

        var levelSession = new LevelSession(timerService);
        levelSession.Start();
    }
}
```

Recommended use:

- composition root
- project bootstrapper
- init modules
- debug/test entry points

Avoid direct resolving from:

- domain models
- app service internals
- view models
- repeated gameplay logic

Prefer constructor or method injection after the project graph has been composed.

## Module Installation Flow

```text
InitialiserBehaviour.Awake()
  -> create ServiceRegistry
  -> create InitModuleInstallContext
  -> read ProjectInitSettingSo.Modules
  -> order modules by InstallOrder, then ModuleName
  -> install each module
  -> mark Initialiser as initialised
```

Destroy flow:

```text
InitialiserBehaviour.OnDestroy()
  -> uninstall installed modules in reverse order
  -> clear installed module list
  -> mark Initialiser as not initialised
```

## Example Init Module

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Timer.App;
using SWeetJelly.Timer.Contract;
using SWeetJelly.Timer.Framework;

public sealed class TimerInitModule : InitModuleSo
{
    private TimerService service;
    private PlayerLoopTimerDriver driver;

    public override string ModuleName => "Timer";

    public override void Install(InitModuleInstallContext context)
    {
        service = new TimerService();
        driver = new PlayerLoopTimerDriver(service);
        driver.Install();

        context.Services.Register<ITimerService>(service);
    }

    public override void Uninstall(InitModuleInstallContext context)
    {
        driver?.Uninstall();
        service?.Dispose();

        context.Services.Unregister<ITimerService>();

        driver = null;
        service = null;
    }
}
```

## Design Notes

- `ServiceRegistry` is intentionally small and type-based.
- `InitModuleSo` should mostly create, wire, register, and unregister services.
- Business logic should live in each feature package's App/Domain layers, not inside init modules.
- `InitialiserBehaviour` is a Unity lifecycle driver, not a domain service.
- Static access through `InitialiserBehaviour.Services` should be limited to bootstrap/composition code.

## Current Status

This package currently provides:

- service registry contracts
- a default `ServiceRegistry`
- ScriptableObject init module workflow
- project init setting asset
- Unity `InitialiserBehaviour`
- install/uninstall lifecycle support
