# CHANGELOG

## v1.0.0

- [Added] Added `SWeetJelly.Initialiser` as an independent Unity package extracted from the previous Core module.
- [Added] Added ScriptableObject-based init module workflow through `InitModuleSo` and `ProjectInitSettingSo`.
- [Added] Added service registration and resolving contracts through `IServiceRegistry` and `IServiceResolver`.
- [Added] Added Unity bootstrap driver through `InitialiserBehaviour`.
- [Changed] Moved from unclassified core module into dedicated Initialiser assemblies.
- [Changed] Separated Initialiser into `Contract`, `App`, and `Framework` layers.
