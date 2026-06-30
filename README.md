# SWeetJelly Bootstrapper

`SWeetJelly Bootstrapper` is a Unity bootstrap package for bootstrapping project packages and sharing the services they create.

It provides a small service registry, ScriptableObject-based package bootstrappers, and a scene `MonoBehaviour` that bootstraps those packages during Unity startup.

## Purpose

Use this package when a Unity project needs one predictable place to compose shared systems such as Timer, Serialization, Save, Loading, Pooling, and project-specific services.

The Bootstrapper package is responsible for:

- creating a shared `ServiceRegistry`
- passing an `PackageBootstrapContext` to each package bootstrapper
- bootstrapping packages in a stable order
- exposing registered services through `BootstrapperBehaviour.Services`
- keeping package bootstrap logic out of gameplay/domain classes

It is not intended to be a global service locator for arbitrary runtime code. Prefer resolving services in a composition root or bootstrapper, then passing dependencies explicitly.

## Package Structure

```text
Runtime/
  Contract/
    IServiceResolver.cs
    IServiceRegistry.cs
    IPackageBootstrapper.cs

  App/
    ServiceRegistry.cs

  Framework/
    BootstrapperBehaviour.cs
    PackageBootstrapperSo.cs
    PackageBootstrapContext.cs
    PackageBootstrapperProfileSo.cs

Editor/
  Framework/
```

## Assemblies

### `SWeetJelly.Bootstrapper.Contract`

Defines the public contracts used by modules and project bootstrap code.

- `IServiceResolver`
- `IServiceRegistry`
- `IPackageBootstrapper`

### `SWeetJelly.Bootstrapper.App`

Contains the Unity-independent registry implementation.

- `ServiceRegistry`

### `SWeetJelly.Bootstrapper.Framework.Unity`

Contains Unity-facing bootstrap types.

- `BootstrapperBehaviour`
- `PackageBootstrapperSo`
- `PackageBootstrapContext`
- `PackageBootstrapperProfileSo`

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

### `PackageBootstrapperSo`

`PackageBootstrapperSo` is the base class for package bootstrappers that create, wire, and register services.

```csharp
public sealed class MyPackageBootstrapper : PackageBootstrapperSo
{
    public override string PackageName => "My Module";

    public override void Bootstrap(PackageBootstrapContext context)
    {
        context.Services.Register<IMyService>(new MyService());
    }

    public override void Shutdown(PackageBootstrapContext context)
    {
        context.Services.Unregister<IMyService>();
    }
}
```

Packages can be ordered with the serialized `BootstrapOrder` value on `PackageBootstrapperSo`. `BootstrapperBehaviour` bootstraps packages by `BootstrapOrder`, then by `PackageName`.

### `PackageBootstrapContext`

The bootstrap context gives each package bootstrapper access to the shared scene/bootstrap objects.

```csharp
public sealed class PackageBootstrapContext
{
    public GameObject HolderObject { get; }
    public Canvas Canvas { get; }
    public EventSystem EventSystem { get; }
    public IServiceRegistry Services { get; }
}
```

Typical usage:

- attach framework behaviours to `HolderObject`
- use `Canvas` for UI-facing package bootstrappers
- use `EventSystem` for input/UI package bootstrappers
- register and resolve shared services through `Services`

### `PackageBootstrapperProfileSo`

`PackageBootstrapperProfileSo` is a ScriptableObject asset that stores the list of package bootstrappers to install.

Create it from:

```text
Create > SWeetJelly > Bootstrapper > Package Bootstrapper Profile
```

Then assign bootstrapper assets to its `PackageBootstrappers` list.

### `BootstrapperBehaviour`

`BootstrapperBehaviour` is the scene entry point.

On `Awake()` it:

1. creates a shared `ServiceRegistry`
2. prepares the `EventSystem` if needed
3. creates an `PackageBootstrapContext`
4. bootstraps packages from `PackageBootstrapperProfileSo`
5. exposes static accessors such as `IsBootstrapped` and `Services`

On destroy, it shuts down bootstrapped packages in reverse order.

## Basic Setup

1. Create a `PackageBootstrapperProfileSo` asset.
2. Create bootstrapper assets such as Timer, Serialization, Save, and Loading package bootstrappers.
3. Add those bootstrapper assets to the package Package Bootstrapper Profile.
4. Add `BootstrapperBehaviour` to an init scene object.
5. Assign the Package Bootstrapper Profile, canvas, and optional event system.

Example scene object:

```text
Bootstrapper
  BootstrapperBehaviour
    Package Bootstrapper Profile: PackageBootstrapperProfileSo
    Canvas: RootCanvas
    Event System: EventSystem
```

## Resolving Services

Project bootstrap code can resolve installed services after the bootstrapper has run.

```csharp
using SWeetJelly.Bootstrapper.Framework.Unity;
using SWeetJelly.Timer.Contract;

public sealed class GameBootstrapBehaviour : MonoBehaviour
{
    private void Start()
    {
        if (!BootstrapperBehaviour.IsBootstrapped)
            throw new InvalidOperationException("Bootstrapper is not bootstrapped.");

        var timerService = BootstrapperBehaviour.Services.Resolve<ITimerService>();

        var levelSession = new LevelSession(timerService);
        levelSession.Start();
    }
}
```

Recommended use:

- composition root
- package bootstrapper composition root
- package bootstrappers
- debug/test entry points

Avoid direct resolving from:

- domain models
- app service internals
- view models
- repeated gameplay logic

Prefer constructor or method injection after the project graph has been composed.

## Module Installation Flow

```text
BootstrapperBehaviour.Awake()
  -> create ServiceRegistry
  -> create PackageBootstrapContext
  -> read PackageBootstrapperProfileSo.PackageBootstrappers
  -> order packages by BootstrapOrder, then PackageName
  -> bootstrap each package
  -> mark Bootstrapper as bootstrapped
```

Destroy flow:

```text
BootstrapperBehaviour.OnDestroy()
  -> shutdown bootstrapped packages in reverse order
  -> clear bootstrapped package list
  -> mark Bootstrapper as not bootstrapped
```

## Example Package Bootstrapper

```csharp
using SWeetJelly.Bootstrapper.Framework.Unity;
using SWeetJelly.Timer.App;
using SWeetJelly.Timer.Contract;
using SWeetJelly.Timer.Framework;

public sealed class TimerPackageBootstrapper : PackageBootstrapperSo
{
    private TimerService service;
    private PlayerLoopTimerDriver driver;

    public override string PackageName => "Timer";

    public override void Bootstrap(PackageBootstrapContext context)
    {
        service = new TimerService();
        driver = new PlayerLoopTimerDriver(service);
        driver.Bootstrap();

        context.Services.Register<ITimerService>(service);
    }

    public override void Shutdown(PackageBootstrapContext context)
    {
        driver?.Shutdown();
        service?.Dispose();

        context.Services.Unregister<ITimerService>();

        driver = null;
        service = null;
    }
}
```

## Design Notes

- `ServiceRegistry` is intentionally small and type-based.
- `PackageBootstrapperSo` should mostly create, wire, register, and unregister services.
- Business logic should live in each feature package's App/Domain layers, not inside package bootstrappers.
- `BootstrapperBehaviour` is a Unity lifecycle driver, not a domain service.
- Static access through `BootstrapperBehaviour.Services` should be limited to bootstrap/composition code.

## Current Status

This package currently provides:

- service registry contracts
- a default `ServiceRegistry`
- ScriptableObject package bootstrapper workflow
- package Package Bootstrapper Profile asset
- Unity `BootstrapperBehaviour`
- bootstrap/shutdown lifecycle support
