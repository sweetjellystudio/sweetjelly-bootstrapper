# CHANGELOG

## v2.0.0 (unreleased)

*   [Changed] Minimum Unity is 6.3 LTS; uGUI dependency is 2.0.0.
*   [Fixed] Duplicate destruction no longer invalidates the active bootstrapper owner.
*   [Fixed] Owner teardown clears published runtime references.
*   [Fixed] Bootstrap failure rolls back completed packages and preserves the original exception.
*   [Fixed] Shutdown errors are logged while remaining packages are still cleaned up.
*   [Fixed] Profile Inspector assembly is Editor-only.
*   [Fixed] Removed undeclared URP camera data and unused AudioListener from the basic sample.
*   [Added] Lifecycle regression tests and maintained agent, architecture and decision documentation.

See README for migration and failure ownership. This is a local candidate, not a published release.

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
