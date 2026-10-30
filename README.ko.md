# SWeetJelly Bootstrapper

`SWeetJelly Bootstrapper`는 Unity 프로젝트 시작 시 여러 패키지의 composition root를 한곳에서 실행하고, 각 패키지가 등록한 service를 공유하기 위한 패키지다.

이 패키지는 다음을 제공한다.

- 공용 `ServiceRegistry`
- ScriptableObject 기반 `PackageBootstrapperSo`
- 실행할 package bootstrapper 목록을 담는 `PackageBootstrapperProfileSo`
- Unity scene entry point인 `BootstrapperBehaviour`
- 각 package bootstrapper에 전달되는 `PackageBootstrapContext`

## 패키지 구조

```text
Runtime/
  Contract/
    IServiceResolver.cs
    IServiceRegistry.cs
    IPackageBootstrapper.cs

  App/
    ServiceRegistry.cs

  Framework/Unity/
    BootstrapperBehaviour.cs
    IUnityPackageBootstrapper.cs
    PackageBootstrapContext.cs
    PackageBootstrapperProfileSo.cs
    PackageBootstrapperSo.cs

Editor/
  Framework/Unity/
    PackageBootstrapperProfileEditor.cs
```

## 주요 타입

### `IPackageBootstrapper`

패키지 부트스트래퍼의 최소 공개 계약이다. `Contract` 레이어에 있으며 Unity 타입을 알지 않는다.

```csharp
public interface IPackageBootstrapper
{
    string PackageName { get; }
}
```

### `IUnityPackageBootstrapper`

Unity runtime에서 실제 bootstrap/shutdown을 실행하기 위한 계약이다. `Framework.Unity` 레이어에 있으며 `PackageBootstrapContext`를 받는다.

```csharp
public interface IUnityPackageBootstrapper : IPackageBootstrapper
{
    int BootstrapOrder { get; }
    void Bootstrap(PackageBootstrapContext context);
    void Shutdown(PackageBootstrapContext context);
}
```

### `PackageBootstrapperSo`

패키지별 조립 코드를 담는 ScriptableObject base class다.

```csharp
public sealed class MyPackageBootstrapperSo : PackageBootstrapperSo
{
    public override string PackageName => "My Package";

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

### `PackageBootstrapperProfileSo`

실행할 `PackageBootstrapperSo` asset 목록을 담는 profile asset이다. Unity Inspector에는 `PackageBootstrapperSo[]`를 직렬화하고, 런타임에는 `IUnityPackageBootstrapper` 목록으로 노출한다.

생성 경로:

```text
Create > SWeetJelly > Bootstrapper > Package Bootstrapper Profile
```

### `BootstrapperBehaviour`

Scene entry point다. `Awake()`에서 다음 흐름을 수행한다.

1. 공용 `ServiceRegistry` 생성
2. 필요한 경우 `EventSystem` 생성
3. `PackageBootstrapContext` 생성
4. `PackageBootstrapperProfileSo.PackageBootstrappers`를 읽음
5. `BootstrapOrder`, `PackageName` 기준으로 정렬
6. 각 package bootstrapper의 `Bootstrap` 실행
7. `Services`, `IsBootstrapped` 같은 static accessor 노출

Destroy 시에는 실행된 package bootstrapper를 역순으로 `Shutdown`한다.

## 기본 설정

1. `PackageBootstrapperProfileSo` asset을 생성한다.
2. 각 패키지의 `PackageBootstrapperSo` asset을 생성한다.
3. profile asset의 package bootstrapper 목록에 추가한다.
4. init scene object에 `BootstrapperBehaviour`를 추가한다.
5. `Package Bootstrapper Profile`, `Canvas`, optional `EventSystem`을 할당한다.

예시 scene object:

```text
Bootstrapper
  BootstrapperBehaviour
    Package Bootstrapper Profile: PackageBootstrapperProfileSo
    Canvas: RootCanvas
    Event System: EventSystem
```

## Service Resolve

Bootstrap 이후 composition root나 테스트 entry point에서 등록된 service를 resolve할 수 있다.

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

## 설계 메모

- `IPackageBootstrapper`는 Unity를 모르는 최소 계약으로 유지한다.
- `IUnityPackageBootstrapper`는 Unity context를 받는 실행 계약이다.
- `PackageBootstrapperProfileSo`는 Unity 직렬화를 위해 구체 `PackageBootstrapperSo[]`를 들고, 외부에는 interface list로 노출한다.
- `BootstrapperBehaviour.Services`를 통한 static 접근은 bootstrap/composition 코드로 제한하는 것이 좋다.
- domain/app 로직은 가능하면 생성자 또는 method injection으로 의존성을 전달받는다.
## 2.0.0 이전과 설치

Unity 6.3 LTS와 com.unity.ugui 2.0.0이 필요합니다. 버전 태그 게시 후 Unity Package Manager에서 https://github.com/sweetjellystudio/sweetjelly-bootstrapper.git#v2.0.0 을 설치합니다. Input System은 선택 사항이며 프로젝트 입력 설정과 선택된 모듈을 맞춰야 합니다. 기존 BaseInputModule은 유지합니다.

기존 profile·package 에셋과 공개 인터페이스는 유지합니다. 실행 중 하나의 BootstrapperBehaviour만 static 상태를 소유합니다. prototype/demo 설정을 따로 두는 방식은 유지하며 동시 독립 runtime scope는 제공하지 않습니다. 무시된 중복 객체의 파괴는 owner에 영향을 주지 않습니다. owner 종료 시 Services·Canvas·GameObject·PackageBootstrapperProfile 참조를 비우지만 설정 에셋은 삭제하지 않습니다.

Bootstrap 실패 시 이후 패키지는 실행하지 않고 성공 완료한 패키지만 역순 정리한 뒤 원래 예외를 유지합니다. 실패한 패키지의 부분 생성 자원은 해당 패키지가 정리해야 합니다. Shutdown 예외는 각각 기록하고 나머지 정리를 계속합니다. 등록 service의 임의 Dispose는 수행하지 않으며 각 패키지가 정리를 소유합니다. Bootstrapped 구독자 예외는 성공 이후의 알림 실패이며 초기화를 rollback하지 않습니다. 구독 해제는 소비자가 담당합니다.

[아키텍처][architecture]와 [수명주기 결정][lifecycle]을 참고하세요. 실제 검증 Editor 패치와 플랫폼은 별도 결과로 기록하며 모든 플랫폼 검증을 의미하지 않습니다.

[architecture]: ARCHITECTURE.md
[lifecycle]: docs/adr/0001-runtime-ownership-and-failure-cleanup.md