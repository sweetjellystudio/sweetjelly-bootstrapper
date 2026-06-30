# CHANGELOG

## v1.0.1

- [Changed] Removed direct compile-time dependency on Unity Input System from `BootstrapperBehaviour`.
- [Changed] `BootstrapperBehaviour` now uses `InputSystemUIInputModule` only when the type is available, otherwise falls back to `StandaloneInputModule`.
- [Changed] Declared `com.unity.ugui` as an explicit package dependency.

## v1.0.0

- [Added] Added `SWeetJelly.Bootstrapper` as an independent Unity package extracted from the previous Core module.
- [Added] Added ScriptableObject-based package bootstrapper workflow through `PackageBootstrapperSo` and `PackageBootstrapperProfileSo`.
- [Added] Added service registration and resolving contracts through `IServiceRegistry` and `IServiceResolver`.
- [Added] Added Unity bootstrap driver through `BootstrapperBehaviour`.
- [Changed] Moved from unclassified core module into dedicated Bootstrapper assemblies.
- [Changed] Separated Bootstrapper into `Contract`, `App`, and `Framework` layers.
