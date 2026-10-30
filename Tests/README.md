# Bootstrapper Tests

이 폴더는 `com.sweetjelly.bootstrapper` 패키지를 현재 Unity 테스트 프로젝트에서 검증하기 위한 테스트를 담는다.

## EditMode

`EditMode` 테스트는 scene 실행 없이 bootstrapper의 순수 app/contract 동작을 검증한다.

- `ServiceRegistryTest`
  - service 등록 후 같은 instance를 resolve할 수 있는지 검증한다.
  - 등록되지 않은 service resolve가 예외를 발생시키는지 검증한다.
  - 같은 service type 중복 등록이 예외를 발생시키는지 검증한다.
  - null service 등록이 예외를 발생시키는지 검증한다.
  - unregister 후 service를 더 이상 resolve할 수 없는지 검증한다.

- `PackageBootstrapContextTest`
  - holder object, canvas, event system, service registry가 context에 그대로 보관되는지 검증한다.
  - holder object 또는 service registry가 null이면 예외를 발생시키는지 검증한다.

## PlayMode

`PlayMode` 테스트는 Unity `MonoBehaviour` lifecycle에서 `BootstrapperBehaviour`가 실제로 초기화 흐름을 수행하는지 검증한다.

- `BootstrapperBehaviourFlowTest`
  - `BootstrapperBehaviour`가 활성화될 때 `PackageBootstrapperProfileSo.PackageBootstrappers`를 bootstrap order와 package name 기준으로 실행하는지 검증한다.
  - destroy 시 bootstrapped package를 역순으로 shutdown하는지 검증한다.
  - static 상태인 `IsBootstrapped`, `GameObject`, `Canvas`, `PackageBootstrapperProfile`, `Services`가 초기화되는지 검증한다.
  - event system reference가 없을 때 child `EventSystem`을 생성하는지 검증한다.

`BootstrapperBehaviour`의 serialized field는 테스트에서 private field reflection으로 주입한다. 실제 프로젝트에서는 Unity Inspector 또는 prefab/scene reference로 설정한다.
## 2.0.0 lifecycle regressions

BootstrapperLifecycleRegressionTest covers duplicate destruction preserving the owner, owner teardown clearing published references, rollback of completed packages on Bootstrap failure, continued cleanup after Shutdown failure, preservation of the original Bootstrap exception when rollback also fails, optional Input System module selection versus legacy fallback, and preservation of an assigned EventSystem with an existing input module. LogAssert.Expect explicitly consumes the deliberate failure-path exceptions; their presence in Console history does not mean the passing suite failed.

Run EditMode and PlayMode separately in the package development host and an isolated consumer with declared uGUI and test tools. For the optional Input System branch, use a separate consumer with that package installed and the active input backend configured appropriately (Both is used by the development verification). Sample import, actual Inspector/scene observation and Player build are separate acceptance checks. Git URL installation is verified after the user publishes the version tag.