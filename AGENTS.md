#   Bootstrapper package instructions

Read [architecture][architecture] before runtime or assembly changes, [usage][usage] before consumer/sample changes, and [lifecycle decision][lifecycle] before changing ownership or failure handling.

*   Preserve public contracts, serialized fields, FormerlySerializedAs mappings and asset GUIDs unless a migration is agreed. Contract/App remain independent of Unity. Framework.Unity owns Unity lifecycle and optional input integration; the Inspector assembly is Editor-only.
*   A runtime bootstrapper owner is distinct from profile and package ScriptableObject assets. Keep ownership guards during initialization, shutdown and duplicate destruction. Asset references must not be deleted when static runtime references are cleared.
*   Run package EditMode and PlayMode tests, including failure rollback, continued shutdown and duplicate ownership. Record exact Editor patch, target, counts and reports. Validate the sample and profile Inspector separately from automated tests.
*   Before release, check root LICENSE.md against manifest, actual asmdef/source dependencies, meta/GUID and Git distribution contents. Test local source with only declared dependencies and test tools. After the user publishes a tag, install its public HTTPS Git URL in an isolated consumer and verify the resolved commit and tests. Tarball/signing is not a required Git delivery step.
*   PRs, commits, pushes, tags, GitHub Releases and game-consumer updates belong to the user. Keep package-specific knowledge here and in maintained docs; shared coordination uses the installed unity-package harness and development-host policy.

[architecture]: ARCHITECTURE.md
[usage]: README.md
[lifecycle]: docs/adr/0001-runtime-ownership-and-failure-cleanup.md
