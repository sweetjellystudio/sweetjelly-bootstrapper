# SWeetJelly Initialiser

`SWeetJelly Initialiser`는 프로젝트 모듈을 설치하고, 그 모듈들이 만든 서비스를 공유하기 위한 Unity 부트스트랩 패키지입니다.

작은 서비스 레지스트리, ScriptableObject 기반 init module, Unity 시작 시점에 모듈을 설치하는 scene `MonoBehaviour`를 제공합니다.

## 목적

Timer, Serialization, Save, Loading, Pooling, 프로젝트 전용 서비스처럼 여러 시스템을 한 곳에서 예측 가능하게 조립해야 할 때 사용합니다.

Initialiser 패키지는 다음을 담당합니다.

- 공유 `ServiceRegistry` 생성
- 각 init module에 `InitModuleInstallContext` 전달
- 안정적인 순서로 모듈 설치
- `InitialiserBehaviour.Services`를 통한 등록 서비스 노출
- gameplay/domain 클래스에서 모듈 구성 로직 분리

임의의 런타임 코드가 전역 서비스 로케이터처럼 사용하는 용도는 아닙니다. 가능하면 composition root나 bootstrapper에서 서비스를 resolve한 뒤, 의존성을 명시적으로 전달하세요.

## 패키지 구조

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

## 어셈블리

### `SWeetJelly.Initialiser.Contract`

모듈과 프로젝트 bootstrap 코드가 사용하는 공개 계약을 정의합니다.

- `IServiceResolver`
- `IServiceRegistry`
- `IInitModule`
- `RegisterModuleAttribute`

### `SWeetJelly.Initialiser.App`

Unity에 의존하지 않는 레지스트리 구현을 포함합니다.

- `ServiceRegistry`

### `SWeetJelly.Initialiser.Framework`

Unity-facing bootstrap 타입을 포함합니다.

- `InitialiserBehaviour`
- `InitModuleSo`
- `InitModuleInstallContext`
- `ProjectInitSettingSo`

## 핵심 타입

### `ServiceRegistry`

`ServiceRegistry`는 서비스를 타입별로 저장합니다.

```csharp
var registry = new ServiceRegistry();

registry.Register<IMyService>(new MyService());

var service = registry.Resolve<IMyService>();
```

같은 서비스 타입을 두 번 등록하거나, 등록되지 않은 서비스를 resolve하면 예외가 발생합니다.

선택적 서비스에는 `TryResolve<T>()`를 사용합니다.

```csharp
if (registry.TryResolve<IMyService>(out var service))
{
    service.Run();
}
```

### `InitModuleSo`

`InitModuleSo`는 서비스를 설치하는 모듈의 base class입니다.

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

모듈 순서는 `InitModuleSo`의 serialized `InstallOrder` 값으로 조정할 수 있습니다. `InitialiserBehaviour`는 `InstallOrder`, 그다음 `ModuleName` 기준으로 모듈을 설치합니다.

### `InitModuleInstallContext`

설치 컨텍스트는 각 모듈에 공유 scene/bootstrap 객체를 제공합니다.

```csharp
public sealed class InitModuleInstallContext
{
    public GameObject HolderObject { get; }
    public Canvas Canvas { get; }
    public EventSystem EventSystem { get; }
    public IServiceRegistry Services { get; }
}
```

일반적인 사용처:

- `HolderObject`에 framework behaviour 부착
- UI-facing 모듈에서 `Canvas` 사용
- input/UI 모듈에서 `EventSystem` 사용
- `Services`를 통해 공유 서비스 등록과 resolve 수행

### `ProjectInitSettingSo`

`ProjectInitSettingSo`는 설치할 init module 목록을 저장하는 ScriptableObject asset입니다.

생성 경로:

```text
Create > SWeetJelly > Settings > Project Init Setting
```

그 뒤 module asset을 `Modules` 목록에 할당합니다.

### `InitialiserBehaviour`

`InitialiserBehaviour`는 scene entry point입니다.

`Awake()`에서 다음을 수행합니다.

1. 공유 `ServiceRegistry` 생성
2. 필요한 경우 `EventSystem` 준비
3. `InitModuleInstallContext` 생성
4. `ProjectInitSettingSo`의 모듈 설치
5. `IsInitialised`, `Services` 같은 정적 accessor 노출

destroy 시에는 설치된 모듈을 역순으로 uninstall합니다.

## 기본 설정

1. `ProjectInitSettingSo` asset을 생성합니다.
2. Timer, Serialization, Save, Loading 같은 module asset을 생성합니다.
3. module asset을 project init setting에 추가합니다.
4. init scene object에 `InitialiserBehaviour`를 추가합니다.
5. init setting, canvas, optional event system을 할당합니다.

예시 scene object:

```text
Initialiser
  InitialiserBehaviour
    Init Setting: ProjectInitSetting
    Canvas: RootCanvas
    Event System: EventSystem
```

## 서비스 Resolve

프로젝트 bootstrap 코드는 initialiser 실행 후 설치된 서비스를 resolve할 수 있습니다.

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

권장 사용처:

- composition root
- project bootstrapper
- init module
- debug/test entry point

피해야 할 사용처:

- domain model
- app service 내부
- view model
- 반복적인 gameplay logic

프로젝트 graph가 조립된 뒤에는 constructor 또는 method injection을 우선하세요.

## 모듈 설치 흐름

```text
InitialiserBehaviour.Awake()
  -> ServiceRegistry 생성
  -> InitModuleInstallContext 생성
  -> ProjectInitSettingSo.Modules 읽기
  -> InstallOrder, ModuleName 기준 정렬
  -> 각 모듈 설치
  -> Initialiser 초기화 완료 표시
```

Destroy 흐름:

```text
InitialiserBehaviour.OnDestroy()
  -> 설치된 모듈을 역순으로 uninstall
  -> 설치 모듈 목록 정리
  -> Initialiser 미초기화 상태로 표시
```

## 설계 메모

- `ServiceRegistry`는 의도적으로 작고 타입 기반입니다.
- `InitModuleSo`는 주로 서비스를 생성, 연결, 등록, 해제하는 역할만 가져야 합니다.
- 비즈니스 로직은 init module이 아니라 각 feature package의 App/Domain layer에 둡니다.
- `InitialiserBehaviour`는 Unity lifecycle driver이지 domain service가 아닙니다.
- `InitialiserBehaviour.Services`를 통한 정적 접근은 bootstrap/composition 코드로 제한하는 것이 좋습니다.

## 현재 상태

이 패키지는 현재 다음을 제공합니다.

- service registry contract
- 기본 `ServiceRegistry`
- ScriptableObject init module workflow
- project init setting asset
- Unity `InitialiserBehaviour`
- install/uninstall lifecycle 지원
