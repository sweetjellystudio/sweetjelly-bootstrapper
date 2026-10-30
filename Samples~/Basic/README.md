# Bootstrapper Dev Sample

This sample shows the consumer-side flow for `com.sweetjelly.bootstrapper`.

It is intentionally small:

- `BootstrapperSamplePackageBootstrapperSo` registers a sample service during Bootstrapper startup.
- `BootstrapperSampleConsumerBehaviour` resolves that service through `BootstrapperBehaviour.Services`.
- `BootstrapperSampleGreeter` is sample-local code, not a reusable package abstraction.

## Setup

1. Create `BootstrapperSamplePackageBootstrapperSo` from `Create > SWeetJelly > Samples > Bootstrapper > Sample Package Bootstrapper`.
2. Create or open a `PackageBootstrapperProfileSo` asset from `Create > SWeetJelly > Bootstrapper > Package Bootstrapper Profile`.
3. Add the Sample Package Bootstrapper to the profile's package bootstrapper list.
4. Add `BootstrapperBehaviour` to a scene object and assign the profile.
5. Add `BootstrapperSampleConsumerBehaviour` to any scene object.
6. Enter Play Mode.

The consumer behaviour logs the resolved service message and draws a tiny `OnGUI` label so the sample works without a custom UI prefab.


The scene uses a plain Camera and does not require URP or Audio modules. The profile and sample package assets retain their existing references. Input setup uses an existing module when assigned; otherwise the bootstrapper selects the optional Input System module or legacy fallback according to the consumer environment.
