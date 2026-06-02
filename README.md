# SWeetJelly Initialiser

`SWeetJelly.Initialiser`는 Unity 프로젝트에서 여러 독립 모듈의 초기화 순서를 관리하고, 초기화 과정에서 생성된 서비스를 공유하기 위한 bootstrap module입니다.

이 모듈은 각 기능 모듈이 제공하는 `InitModuleSo`를 순차적으로 실행하고, `IServiceRegistry`를 통해 공통 서비스들을 등록/조회할 수 있게 합니다.

예를 들어 `Timer`, `Save`, `Loading`, `Pooling` 같은 모듈은 각자의 `InitialiserIntegration` assembly에서 `InitModuleSo`를 제공할 수 있으며, 프로젝트는 `ProjectInitSettingSo`에 필요한 init module들을 등록하는 방식으로 bootstrap 흐름을 구성할 수 있습니다.

---

## Purpose

이 모듈의 목적은 다음과 같습니다.

- Unity 시작 시 공통 모듈들을 순서대로 초기화한다.
- 각 모듈이 생성한 서비스를 `IServiceRegistry`에 등록한다.
- 프로젝트 bootstrapper가 등록된 서비스를 `IServiceResolver`를 통해 가져와 사용할 수 있게 한다.
- 모듈 초기화 로직을 scene bootstrapper에서 분리한다.
- Unity `ScriptableObject` 기반으로 초기화 구성을 asset화한다.

---

## Architecture

```plantuml
package "SWeetJelly.Initialiser.Contract" {
  interface IServiceRegistry
  interface IServiceResolver
  class RegisterModuleAttribute
}

package "SWeetJelly.Initialiser.App" {
  class ServiceRegistry
}

package "SWeetJelly.Initialiser.Framework" {
  class InitialiserBehaviour
  abstract InitModuleSo
  class InitModuleInstallContext
  class ProjectInitSettingSo
}
```

## Assemblies

### `SWeetJelly.Initialiser.Contract`

공통 계약을 정의합니다.

포함 요소:

* `IServiceResolver`
* `IServiceRegistry`
* `RegisterModuleAttribute`

이 assembly는 다른 모듈이나 프로젝트 코드에서 참조할 수 있는 최소 public API를 제공합니다.

### `SWeetJelly.Initialiser.App`

Unity에 의존하지 않는 application-level 구현을 포함합니다.

포함 요소:

* `ServiceRegistry`

`ServiceRegistry`는 `IServiceRegistry` 구현체이며, 초기화된 서비스를 type 기반으로 등록하고 조회합니다.

### `SWeetJelly.Initialiser.Framework`

Unity 의존 초기화 흐름을 담당합니다.

포함 요소:

* `InitialiserBehaviour`
* `InitModuleSo`
* `InitModuleInstallContext`
* `ProjectInitSettingSo`

이 assembly는 Unity scene, `MonoBehaviour`, `ScriptableObject`, `Canvas`, `EventSystem` 등에 의존합니다.

## Core Concepts

### `IServiceResolver`

등록된 서비스를 조회하기 위한 읽기 전용 인터페이스입니다.

```csharp
public interface IServiceResolver
{
    T Resolve<T>() where T : class;
    bool TryResolve<T>(out T service) where T : class;
}
```

일반적인 project bootstrapper나 app service composition 단계에서는 이 인터페이스를 사용합니다.

---

### `IServiceRegistry`

서비스를 등록/해제하기 위한 인터페이스입니다.

```csharp
public interface IServiceRegistry : IServiceResolver
{
    void Register<T>(T service) where T : class;
    void Unregister<T>() where T : class;
}
```

주로 `InitModuleSo.Install()` 내부에서 사용합니다.

### `ServiceRegistry`

`IServiceRegistry`의 기본 구현체입니다.

```csharp
public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<Type, object> services = new();

    public void Register<T>(T service) where T : class;
    public T Resolve<T>() where T : class;
    public bool TryResolve<T>(out T service) where T : class;
    public void Unregister<T>() where T : class;
}
```

`ServiceRegistry`는 Unity에 의존하지 않으며, Initialiser의 app-level service container 역할을 합니다.

> `ServiceRegistry`는 일반 코드에서 남용하기 위한 service locator가 아닙니다.
> 사용 위치는 composition root, init module, bootstrapper로 제한하는 것을 권장합니다.

### `InitModuleSo`

Unity `ScriptableObject` 기반 초기화 모듈입니다.

각 기능 모듈은 자신만의 init module을 제공할 수 있습니다.

```csharp
public abstract class InitModuleSo : ScriptableObject
{
    public abstract string ModuleName { get; }

    public abstract void Install(InitModuleInstallContext context);

    public virtual void Uninstall(InitModuleInstallContext context)
    {
    }
}
```

예를 들어 Timer 모듈은 `TimerInitModuleSo`를 제공할 수 있습니다.

```csharp
[CreateAssetMenu(
    fileName = "TimerInitModule",
    menuName = "SWeetJelly/Initialiser/Modules/Timer")]
public sealed class TimerInitModuleSo : InitModuleSo
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

### `InitModuleInstallContext`

각 init module이 설치될 때 전달되는 context입니다.

```csharp
public sealed class InitModuleInstallContext
{
    public GameObject HolderObject { get; }
    public Canvas Canvas { get; }
    public EventSystem EventSystem { get; }
    public IServiceRegistry Services { get; }
}
```

이 context를 통해 init module은 다음 정보를 사용할 수 있습니다.

* bootstrap holder object
* root canvas
* event system
* service registry

### `ProjectInitSettingSo`

프로젝트에서 사용할 init module 목록을 보관하는 설정 asset입니다.

```csharp
[CreateAssetMenu(
    fileName = "ProjectInitSetting",
    menuName = "SWeetJelly/Initialiser/Project Init Setting")]
public sealed class ProjectInitSettingSo : ScriptableObject
{
    [SerializeField] private List<InitModuleSo> modules;

    public IReadOnlyList<InitModuleSo> Modules => modules;
}
```

프로젝트는 이 asset에 필요한 init module들을 등록합니다.

예시:

```text
ProjectInitSettingSo
  - TimerInitModuleSo
  - SaveInitModuleSo
  - LoadingInitModuleSo
  - PoolingInitModuleSo
```

### `InitialiserBehaviour`

Unity scene에서 초기화 흐름을 실행하는 driver입니다.

`InitialiserBehaviour`는 init scene에 배치되며, `Awake()`에서 `ProjectInitSettingSo`에 등록된 `InitModuleSo`들을 순서대로 설치합니다.

```csharp
[DefaultExecutionOrder(-999)]
public sealed class InitialiserBehaviour : MonoBehaviour
{
    [SerializeField] private ProjectInitSettingSo initSetting;
    [SerializeField] private Canvas canvas;
    [SerializeField] private EventSystem eventSystem;

    private readonly ServiceRegistry serviceRegistry = new();

    public static bool IsInitialised { get; private set; }
    public static IServiceResolver Services { get; private set; }

    private void Awake()
    {
        Initialise();
    }
}
```

`InitialiserBehaviour`는 모듈의 실제 기능을 구현하지 않습니다.
역할은 Unity scene에서 초기화 파이프라인을 실행하고, 각 module에 install context를 전달하는 것입니다.

## Usage

### 1. Create an Init Module

각 기능 모듈은 InitialiserIntegration assembly에서 `InitModuleSo`를 구현합니다.

```csharp
public sealed class TimerInitModuleSo : InitModuleSo
{
    public override string ModuleName => "Timer";

    public override void Install(InitModuleInstallContext context)
    {
        var service = new TimerService();

        context.Services.Register<ITimerService>(service);
    }
}
```

### 2. Create a Project Init Setting

Unity Editor에서 `ProjectInitSettingSo` asset을 생성합니다.

```text
Create > SWeetJelly > Initialiser > Project Init Setting
```

그리고 사용할 init module asset들을 등록합니다.

```text
Modules
  0. TimerInitModule
  1. SaveInitModule
  2. LoadingInitModule
  3. PoolingInitModule
```

### 3. Add InitialiserBehaviour to Init Scene

init scene에 `InitialiserBehaviour`를 배치합니다.

필요한 reference를 할당합니다.

```text
InitialiserBehaviour
  Init Setting: ProjectInitSettingSo
  Canvas: Root Canvas
  Event System: EventSystem
```

게임이 시작되면 `InitialiserBehaviour.Awake()`에서 등록된 init module들이 설치됩니다.

### 4. Resolve Services from Project Bootstrapper

프로젝트 bootstrapper는 Initialiser가 등록한 서비스를 가져와 project-level service graph를 구성할 수 있습니다.

```csharp
public sealed class GameBootstrapBehaviour : MonoBehaviour
{
    private IServiceResolver services;

    private void Awake()
    {
        if (!InitialiserBehaviour.IsInitialised)
            throw new InvalidOperationException("Initialiser is not initialised.");

        services = InitialiserBehaviour.Services;

        var timerService = services.Resolve<ITimerService>();

        var levelService = new LevelService(
            levelRepository,
            levelModelFactory,
            rewardService,
            timerService,
            appLifetime);
    }
}
```

권장 규칙:

```text
Allowed:
  ProjectBootstrapper
  InitModuleSo
  Composition Root
  Debug Entry Point

Avoid:
  Domain Model
  App Service internal logic
  Session internal logic
  ViewModel
```

즉, `ServiceRegistry`는 composition 단계에서만 사용하고, 실제 app service나 session에는 생성자 주입으로 전달하는 것을 권장합니다.

---

## Service Access Rule

좋은 예:

```csharp
public sealed class LevelService
{
    private readonly ITimerService timerService;

    public LevelService(ITimerService timerService)
    {
        this.timerService = timerService;
    }
}
```

나쁜 예:

```csharp
public sealed class LevelService
{
    public void Start()
    {
        var timer = InitialiserBehaviour.Services.Resolve<ITimerService>();
    }
}
```

`Resolve()`는 project bootstrapper 같은 composition root에서만 사용하고, 실제 서비스 객체에는 명시적으로 주입하는 것이 좋습니다.

---

## Module Installation Flow

```text
InitialiserBehaviour.Awake()
  -> Create ServiceRegistry
  -> Create InitModuleInstallContext
  -> Read ProjectInitSettingSo.Modules
  -> Install TimerInitModuleSo
      -> Register ITimerService
  -> Install SaveInitModuleSo
      -> Register ISaveService
  -> Install LoadingInitModuleSo
      -> Register ILoadingService
  -> Mark Initialiser as initialised
```

이후 project bootstrapper는 등록된 서비스를 사용해 project-specific graph를 조립합니다.

```text
GameBootstrapBehaviour.Awake()
  -> Resolve ITimerService
  -> Resolve ISaveService
  -> Build Project Domain Services
  -> Build Project App Services
  -> Build Presentation Services
  -> Build GameUseCaseOrchestrator
```

---

## Recommended Layering

각 기능 모듈은 다음 구조를 가질 수 있습니다.

```text
SWeetJelly.Timer.Contract
SWeetJelly.Timer.Domain
SWeetJelly.Timer.App
SWeetJelly.Timer.Framework
SWeetJelly.Timer.InitialiserIntegration
```

예시:

```text
SWeetJelly.Timer.Contract
  ITimerService
  ITimerHandle

SWeetJelly.Timer.App
  TimerService

SWeetJelly.Timer.Domain
  TimerBase
  CountdownTimer
  FrequencyTimer
  StopwatchTimer

SWeetJelly.Timer.Framework
  PlayerLoopTimerDriver

SWeetJelly.Timer.InitialiserIntegration
  TimerInitModuleSo
```

`TimerInitModuleSo`는 Timer 모듈을 Initialiser에 연결하는 adapter입니다.

---

## Design Notes

### InitialiserBehaviour is a Unity Driver

`InitialiserBehaviour`는 Initialiser의 core logic이 아니라 Unity lifecycle에서 초기화 흐름을 시작하는 driver입니다.

```text
InitialiserBehaviour
  = Unity entry point
  = InitModuleSo installer
  = ServiceRegistry provider
```

---

### InitModuleSo is a Unity Adapter

`InitModuleSo`는 `ScriptableObject` 기반 Unity adapter입니다.

모듈 자체의 핵심 기능은 `App`, `Domain`, `Framework` layer에 두고, `InitModuleSo`는 해당 기능을 생성하고 등록하는 역할만 수행합니다.

```text
TimerService
  = Timer.App

PlayerLoopTimerDriver
  = Timer.Framework

TimerInitModuleSo
  = Timer.InitialiserIntegration
  = TimerService와 PlayerLoopTimerDriver를 조립하고 등록
```

---

### ServiceRegistry is not the Application Logic

`ServiceRegistry`는 서비스를 보관하는 registry일 뿐, business logic을 담지 않습니다.

서비스 사용자는 가능한 한 직접 `Resolve()`하지 않고, 생성자를 통해 필요한 service contract를 전달받아야 합니다.

---

## Uninstall

각 init module은 필요하면 `Uninstall()`을 구현할 수 있습니다.

```csharp
public override void Uninstall(InitModuleInstallContext context)
{
    context.Services.Unregister<ITimerService>();

    driver?.Uninstall();
    service?.Dispose();
}
```

`InitialiserBehaviour`는 종료 시 설치된 module들을 역순으로 uninstall하는 것을 권장합니다.

```text
Install:
  Timer
  Save
  Project

Uninstall:
  Project
  Save
  Timer
```

---

## Example

```csharp
public interface ITimerService
{
    ITimerHandle StartFrequency(float interval, Action onTick);
}
```

```csharp
public sealed class TimerInitModuleSo : InitModuleSo
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
    }
}
```

```csharp
public sealed class GameBootstrapBehaviour : MonoBehaviour
{
    private IServiceResolver services;

    private void Awake()
    {
        var timer = services.Resolve<ITimerService>();

        var levelService = new LevelService(timer);
    }
}
```

---

## Guidelines

* `InitModuleSo`는 서비스 생성과 등록만 담당합니다.
* `InitModuleSo`에 project-specific game flow를 많이 넣지 않습니다.
* Project bootstrapper는 Initialiser가 등록한 services를 resolve해서 project graph를 구성합니다.
* Domain layer에서는 `ServiceRegistry`를 사용하지 않습니다.
* App service와 session에서는 `ServiceRegistry`를 직접 resolve하지 않습니다.
* 필요한 service는 생성자 주입으로 전달합니다.
* Unity scene object reference는 `MonoBehaviour` bootstrapper 또는 scene context에서 관리합니다.
* `ScriptableObject` init module에는 runtime mutable state를 최소화합니다.

---

## Current Status

This module currently provides:

* Service registry contract
* Service registry implementation
* Unity initialiser behaviour
* ScriptableObject-based init module workflow
* Project init setting asset
* Init module install context

Future improvements may include:

* Editor tooling for discovering init modules
* automatic init module asset generation
* validation for duplicate module registration
* install/uninstall failure rollback
* async boot phase support
