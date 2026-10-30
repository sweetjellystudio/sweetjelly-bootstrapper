#   Bootstrapper architecture

Bootstrapper runs package composition roots and shares a service registry. It does not own consumer service implementations, automatic service disposal, or a game's scene navigation. See [usage][usage] and the [lifecycle decision][lifecycle].

##  Boundaries

Contract owns IServiceResolver, IServiceRegistry and the minimal IPackageBootstrapper identity. App implements ServiceRegistry with no Unity references. Framework.Unity owns the Unity execution contract, context, ScriptableObject configuration and BootstrapperBehaviour. It depends on Contract/App and uGUI EventSystem; Input System remains optional through runtime type lookup. Editor owns the profile Inspector and must not enter a Player compilation. Inspect actual asmdefs before changing dependencies.

##  Runtime ownership

One BootstrapperBehaviour owns the static runtime state per execution. Prototype/demo profile assets can coexist on disk; this is not simultaneous independent runtime scopes. The owner is claimed before package callbacks, so nested or duplicate bootstrappers cannot replace it. A duplicate does not initialize, and its destruction does not clear the owner's state.

Awake creates the context, selects an input module if none exists, and initializes packages by BootstrapOrder then PackageName. Null entries are ignored. Successfully completed packages are retained for reverse shutdown. During initialization Services exposes the registry as in 1.x; IsBootstrapped becomes true only after all package initializers complete. Bootstrapped is a success notification after that point. Subscriber exceptions propagate without rolling back a successful bootstrap. Subscribers own unsubscription; the event is not automatically erased on shutdown.

Bootstrap failure stops later initializers, shuts down completed packages in reverse order, clears published static references and rethrows the original exception. A package whose Bootstrap throws owns cleanup of its own partially created resources. Shutdown logs individual exceptions and continues to remaining packages. Owner teardown clears static state and context; it does not delete configuration assets or dispose arbitrary services retained by callers. Each package Shutdown owns its resources and registrations.

Input setup preserves an existing BaseInputModule, otherwise chooses InputSystemUIInputModule when its type is installed and falls back to StandaloneInputModule. This does not change project active-input settings. Consumers configure the appropriate backend; this package has no hard Input System dependency.

##  Compatibility and verification

2.0.0 raises the minimum Editor to Unity 6.3 LTS and uGUI to 2.0.0. Public interfaces, serialized field names and assets remain compatible; static cleanup and failure handling intentionally change stale/error paths. Registry duplicate/missing-service exceptions are preserved. Operations are synchronous Unity-main-thread lifecycle callbacks with no cancellation API or async completion contract.

Package tests own correctness; the development host checks combinations with debug, timer, pooling, serialization, save, loading and tween. Samples~/Basic is a thin service consumer. Verify EditMode, PlayMode, sample/Inspector observation and isolated consumption independently, naming actual Editor patch and platform. Player compilation separately checks that UnityEditor code does not leak into runtime. Other versions/platforms require their own evidence.

[usage]: README.md
[lifecycle]: docs/adr/0001-runtime-ownership-and-failure-cleanup.md
