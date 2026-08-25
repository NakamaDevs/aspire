# Elixir integration parity matrix

This document lists every test in the Go, Python, JavaScript, Java, Rust, and TypeScript hosting and code-generation test projects. For each test, it names the matching Elixir test. The Elixir integration reaches full compatibility when every matching test exists and passes. This document is the definition of done for the Elixir integration (NAK-505).

## How to update this document

Set a row's Status to `written` when the matching test exists in the target Elixir test project. Do not change Status to `passing`; `passing` is a Summary-table count, not a row status. Update the Passing count in the Summary table once `dotnet test` or `mix test` reports the test green. Rename the Elixir test in this table when the real test gets a clearer name during review. Add a new row when a new source test lands. Do not delete a row when the team removes a source test. Mark it `n/a — source test removed` instead, so the history stays visible.

The Elixir test names in this table come from a mechanical, best-effort translation of the source test names. They are a starting point, not a fixed contract. Update the name in this table to match the real Elixir test after a rename.

## Summary

| Source project | Total | Planned | Written | Passing | n/a |
|---|---:|---:|---:|---:|---:|
| Go | 94 | 59 | 15 | 0 | 20 |
| Python | 96 | 90 | 3 | 0 | 3 |
| JavaScript | 169 | 152 | 6 | 0 | 11 |
| Java | 237 | 193 | 1 | 0 | 43 |
| Rust CodeGen | 24 | 24 | 0 | 0 | 0 |
| Go CodeGen | 25 | 25 | 0 | 0 | 0 |
| Python CodeGen | 22 | 22 | 0 | 0 | 0 |
| Java CodeGen | 40 | 40 | 0 | 0 | 0 |
| TypeScript CodeGen | 114 | 114 | 0 | 0 | 0 |
| TypeScript JsTests (vitest) | 28 | 28 | 0 | 0 | 0 |
| CLI E2E Polyglot | 7 | 6 | 0 | 0 | 1 |
| **Total** | **856** | **753** | **25** | **0** | **78** |

Passing starts at 0 for every project. This document does not run any test. Update the Passing count by hand after a verification run. Record the run date in the pull request that updates this file.

## Milestones and tickets

M1 adds the Elixir hosting integration, `Aspire.Hosting.Elixir`. This runtime package lets an AppHost add and run an Elixir/Mix application. M2 adds Elixir as a supported AppHost language, `Aspire.Hosting.CodeGeneration.Elixir`. M2 lets a developer write the AppHost itself in Elixir. See the linked Linear issues for the scope of each milestone.

## Go

M1 source: `tests/Aspire.Hosting.Go.Tests`. Target: `tests/Aspire.Hosting.Elixir.Tests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AddGoAppTests.VerifyManifest_GoRunDot | VerifyManifest_AddElixirApp | NAK-489 | written |
| AddGoAppTests.VerifyManifest_AddGoApp_PackagePath | VerifyManifest_AddElixirApp_AppDirectory | NAK-492 | planned |
| AddGoAppTests.VerifyPublish_PackagePath_UsedInDockerfileBuildCommand | VerifyPublish_AppDirectory_UsedInDockerfileBuildCommand | NAK-498 | planned |
| AddGoAppTests.VerifyManifest_AddGoApp_BuildTagsParam | VerifyManifest_AddElixirApp_MixEnvParam | NAK-492 | planned |
| AddGoAppTests.VerifyManifest_AddGoApp_LdFlagsParam | n/a — the BEAM has no linker; there is no LD-flags equivalent to inject | NAK-492 | n/a |
| AddGoAppTests.VerifyManifest_AddGoApp_GcFlagsParam | n/a — the BEAM has no native code generator; there is no GC-flags equivalent to inject | NAK-492 | n/a |
| AddGoAppTests.VerifyManifest_AddGoApp_RaceDetectorParam | n/a — the BEAM scheduler has no data-race detector comparable to Go's -race flag | NAK-492 | n/a |
| AddGoAppTests.VerifyManifest_AddGoApp_AllBuildParams | VerifyManifest_AddElixirApp_AllRunParams | NAK-492 | planned |
| AddGoAppTests.VerifyManifest_WithAppArgs | VerifyManifest_WithAppArgs | NAK-489 | written |
| AddGoAppTests.VerifyManifest_WithModTidy_DoesNotAlterMainManifest | VerifyManifest_WithMixDeps_DoesNotAlterMainManifest | NAK-491 | planned |
| AddGoAppTests.VerifyManifest_WithModVendor_DoesNotAlterMainManifest | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| AddGoAppTests.VerifyManifest_WithModDownload_DoesNotAlterMainManifest | VerifyManifest_WithMixDeps_DoesNotAlterMainManifest | NAK-491 | planned |
| AddGoAppTests.VerifyManifest_WithDelveServer | VerifyManifest_WithElixirLsServer | NAK-497 | planned |
| AddGoAppTests.VerifyManifest_WithDelveServer_EnableAcceptMultiClient | n/a — dlv's multi-client flag has no ElixirLS Debug Adapter Protocol equivalent | NAK-497 | n/a |
| AddGoAppTests.VerifyManifest_WithDelveServer_DisableOnlySameUser | n/a — dlv's only-same-user socket restriction has no ElixirLS Debug Adapter Protocol equivalent | NAK-497 | n/a |
| AddGoAppTests.VerifyManifest_WithDelveServer_ContinueOnStart | n/a — dlv's continue-on-start flag has no ElixirLS Debug Adapter Protocol equivalent | NAK-497 | n/a |
| AddGoAppTests.VerifyManifest_WithDelveServer_EnableLog | n/a — dlv's server-log flag has no ElixirLS Debug Adapter Protocol equivalent | NAK-497 | n/a |
| AddGoAppTests.WithDelveServer_UsesDelveCommandWhenGoLaunchConfigurationIsSupported | WithElixirLsServer_UsesElixirLsCommandWhenElixirLaunchConfigurationIsSupported | NAK-497 | planned |
| AddGoAppTests.WithVSCodeDebugging_PopulatesGoLaunchConfiguration | WithVSCodeDebugging_PopulatesElixirLaunchConfiguration | NAK-497 | planned |
| AddGoAppTests.WithVSCodeDebugging_OmitsBuildFlagsWhenNoneConfigured | WithVSCodeDebugging_OmitsCompileFlagsWhenNoneConfigured | NAK-497 | planned |
| AddGoAppTests.WithVSCodeDebugging_KeepsGoToolArgumentsInTheAppModel | WithVSCodeDebugging_KeepsMixTaskArgumentsInTheAppModel | NAK-497 | planned |
| AddGoAppTests.WithVSCodeDebugging_GoToolArgumentsLeadTheCommandLineRegardlessOfCallOrder | WithVSCodeDebugging_MixTaskArgumentsLeadTheCommandLineRegardlessOfCallOrder | NAK-497 | planned |
| AddGoAppTests.WithVSCodeDebugging_DoesNotRemoveGoToolArguments_WhenGoLaunchConfigurationUnsupported | WithVSCodeDebugging_DoesNotRemoveMixTaskArguments_WhenElixirLaunchConfigurationUnsupported | NAK-497 | planned |
| AddGoAppTests.VerifyManifest_WithDelveServer_AndBuildFlags | VerifyManifest_WithElixirLsServer_AndCompileFlags | NAK-497 | planned |
| AddGoAppTests.VerifyManifest_WithDelveServer_AndRaceDetector | n/a — no BEAM equivalent for this Go compiler/runtime flag | NAK-497 | n/a |
| AddGoAppTests.VerifyManifest_WithDelveServer_AndGcFlags | n/a — no BEAM equivalent for this Go compiler/runtime flag | NAK-497 | n/a |
| AddGoAppTests.VerifyManifest_WithDelveServer_AndAppArgs | VerifyManifest_WithElixirLsServer_AndAppArgs | NAK-497 | planned |
| AddGoAppTests.VerifyPublish_GeneratesDockerfile_WithGoVersionFromGoMod | VerifyPublish_GeneratesDockerfile_WithGoVersionFromToolVersions | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_UsesDefaultGoVersion_WhenGoModAbsent | VerifyPublish_UsesDefaultGoVersion_WhenToolVersionsAbsent | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_PropagatesBuildFlagsToDockerfile | VerifyPublish_PropagatesCompileFlagsToDockerfile | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_ShellQuote_HandlesEmbeddedSingleQuotes | VerifyPublish_ShellQuote_HandlesEmbeddedSingleQuotes | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_SkipsDockerfileGeneration_WhenDockerfileExists | VerifyPublish_SkipsDockerfileGeneration_WhenDockerfileExists | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_RespectsDockerfileBaseImageAnnotation | VerifyPublish_RespectsDockerfileBaseImageAnnotation | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_WithGoPrivate_GeneratesNetrcAndGoprivate | VerifyPublish_WithHexOrganizationAuth_GeneratesNetrcAndGoprivate | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_WithGoPrivate_CustomTokenSecretId | VerifyPublish_WithHexOrganizationAuth_CustomTokenSecretId | NAK-498 | planned |
| AddGoAppTests.GoAppResource_ImplementsIContainerFilesDestinationResource | ElixirAppResource_ImplementsIContainerFilesDestinationResource | NAK-489 | written |
| AddGoAppTests.PublishWithContainerFiles_AddsAnnotationToGoResource | PublishWithContainerFiles_AddsAnnotationToGoResource | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_ContainerFiles_GeneratesFromAndCopyInstructions | VerifyPublish_ContainerFiles_GeneratesFromAndCopyInstructions | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_ContainerFiles_MultipleSourcesAllPresent | VerifyPublish_ContainerFiles_MultipleSourcesAllPresent | NAK-498 | planned |
| AddGoAppTests.AddGoApp_HasRequiredCommandAnnotationForGo | AddElixirApp_HasRequiredCommandAnnotationForMixAndElixir | NAK-489 | written |
| AddGoAppTests.WithDelveServer_AddsRequiredCommandAnnotationForDlv | WithElixirLsServer_AddsRequiredCommandAnnotationForElixirLs | NAK-497 | planned |
| AddGoAppTests.VerifyPublish_RaceDetector_NotPropagatedToDockerfile | n/a — the BEAM scheduler has no data-race detector comparable to Go's -race flag | NAK-498 | n/a |
| AddGoAppTests.WithModTidy_ThenWithModVendor_VendorWaitsForTidy | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| AddGoAppTests.WithModTidy_ThenWithModDownload_DownloadWaitsForTidy | WithMixDeps_ThenWithMixDeps_DownloadWaitsForTidy | NAK-491 | planned |
| AddGoAppTests.WithModTidy_ThenWithModVendor_ThenWithModDownload_DownloadWaitsForVendor | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| AddGoAppTests.VerifyPublish_RuntimeStage_HasNonRootUser_Alpine | VerifyPublish_RuntimeStage_HasNonRootUser_Alpine | NAK-498 | planned |
| AddGoAppTests.VerifyPublish_RuntimeStage_HasNonRootUser_NonAlpine | VerifyPublish_RuntimeStage_HasNonRootUser_NonAlpine | NAK-498 | planned |
| GoPublicApiTests.CtorGoAppResourceShouldThrowWhenNameIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| GoPublicApiTests.CtorGoAppResourceShouldThrowWhenWorkingDirectoryIsNull | CtorElixirAppResourceShouldThrowWhenWorkingDirectoryIsNull | NAK-489 | written |
| GoPublicApiTests.AddGoAppShouldThrowWhenBuilderIsNull | AddElixirAppShouldThrowWhenBuilderIsNull | NAK-489 | written |
| GoPublicApiTests.AddGoAppShouldThrowWhenNameIsNullOrEmpty | AddElixirAppShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| GoPublicApiTests.AddGoAppShouldThrowWhenAppDirectoryIsNullOrEmpty | AddElixirAppShouldThrowWhenAppDirectoryIsNullOrEmpty | NAK-489 | written |
| GoPublicApiTests.AddGoAppUsesGoAsCommand | AddElixirAppUsesMixAsCommand | NAK-489 | written |
| GoPublicApiTests.AddGoAppDefaultArgsAreRunDot | AddElixirAppDefaultArgsAreRunNoHalt | NAK-489 | written |
| GoPublicApiTests.AddGoApp_PackagePath_DefaultsToRunDot | AddElixirApp_AppDirectory_DefaultsToRunNoHalt | NAK-492 | planned |
| GoPublicApiTests.AddGoApp_PackagePath_UsedInRunMode | AddElixirApp_AppDirectory_UsedInRunMode | NAK-492 | planned |
| GoPublicApiTests.AddGoApp_PackagePath_UsedInDelveMode | AddElixirApp_AppDirectory_UsedInElixirLsMode | NAK-497 | planned |
| GoPublicApiTests.AddGoApp_PackagePath_CombinedWithBuildFlags | AddElixirApp_AppDirectory_CombinedWithCompileFlags | NAK-492 | planned |
| GoPublicApiTests.AddGoApp_BuildTagsParam_InjectsTagsFlag | AddElixirApp_MixEnvParam_InjectsTagsFlag | NAK-492 | planned |
| GoPublicApiTests.AddGoApp_LdFlagsParam_InjectsLdFlagsArg | n/a — the BEAM has no linker; there is no LD-flags equivalent to inject | NAK-492 | n/a |
| GoPublicApiTests.AddGoApp_GcFlagsParam_InjectsGcFlagsArg | n/a — the BEAM has no native code generator; there is no GC-flags equivalent to inject | NAK-492 | n/a |
| GoPublicApiTests.AddGoApp_RaceDetectorParam_InjectsRaceFlag | n/a — the BEAM scheduler has no data-race detector comparable to Go's -race flag | NAK-492 | n/a |
| GoPublicApiTests.AddGoApp_AllBuildParams_ProduceCorrectOrdering | AddElixirApp_AllRunParams_ProduceCorrectOrdering | NAK-492 | planned |
| GoPublicApiTests.WithAppArgsShouldThrowWhenBuilderIsNull | WithAppArgsShouldThrowWhenBuilderIsNull | NAK-489 | written |
| GoPublicApiTests.WithAppArgsPassesArgsAfterDot | WithAppArgsPassesArgsAfterSeparator | NAK-489 | written |
| GoPublicApiTests.WithAppArgs_AcceptsReferenceExpression | WithAppArgs_AcceptsReferenceExpression | NAK-489 | written |
| GoPublicApiTests.WithAppArgsReplacesOnSecondCall | WithAppArgsReplacesOnSecondCall | NAK-489 | written |
| GoPublicApiTests.WithModTidyShouldThrowWhenBuilderIsNull | WithMixDepsShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| GoPublicApiTests.WithModTidyIsIdempotent | WithMixDepsIsIdempotent | NAK-491 | planned |
| GoPublicApiTests.WithModTidyCreatesSiblingResource | WithMixDepsCreatesSiblingResource | NAK-491 | planned |
| GoPublicApiTests.WithModVendorShouldThrowWhenBuilderIsNull | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| GoPublicApiTests.WithModVendorIsIdempotent | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| GoPublicApiTests.WithModVendorCreatesSiblingResource | n/a — mix has no vendor step; mix deps.get already caches dependencies under deps/ | NAK-491 | n/a |
| GoPublicApiTests.WithModDownloadShouldThrowWhenBuilderIsNull | WithMixDepsShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| GoPublicApiTests.WithModDownloadIsIdempotent | WithMixDepsIsIdempotent | NAK-491 | planned |
| GoPublicApiTests.WithModDownloadCreatesSiblingResource | WithMixDepsCreatesSiblingResource | NAK-491 | planned |
| GoPublicApiTests.WithVetToolShouldThrowWhenBuilderIsNull | WithMixTaskShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| GoPublicApiTests.WithVetToolIsIdempotent | WithMixTaskIsIdempotent | NAK-491 | planned |
| GoPublicApiTests.WithVetToolCreatesSiblingResource | WithMixTaskCreatesSiblingResource | NAK-491 | planned |
| GoPublicApiTests.WithDelveServerShouldThrowWhenBuilderIsNull | WithElixirLsServerShouldThrowWhenBuilderIsNull | NAK-497 | planned |
| GoPublicApiTests.WithDelveServerNullOptionsUseDefaults | WithElixirLsServerNullOptionsUseDefaults | NAK-497 | planned |
| GoPublicApiTests.DelveServerOptionsHaveSafeDefaults | ElixirLsServerOptionsHaveSafeDefaults | NAK-497 | planned |
| GoPublicApiTests.WithDelveServerSwitchesCommandToDlv | WithElixirLsServerSwitchesCommandToElixirLs | NAK-497 | planned |
| GoPublicApiTests.WithDelveServerProducesCorrectArgs | WithElixirLsServerProducesCorrectArgs | NAK-497 | planned |
| GoPublicApiTests.ObsoleteWithDelveServerPortOverloadPreservesBehavior | n/a — tests a Go-specific obsolete API overload; the new Elixir integration has no legacy overload to preserve | NAK-497 | n/a |
| GoPublicApiTests.WithDelveServerOptionsProduceCorrectArgs | WithElixirLsServerOptionsProduceCorrectArgs | NAK-497 | planned |
| GoPublicApiTests.WithDelveServerIncludesBuildFlagsWhenPresent | WithElixirLsServerIncludesCompileFlagsWhenPresent | NAK-497 | planned |
| GoPublicApiTests.WithDelveServerPassesAppArgsAfterDoubleDash | WithElixirLsServerPassesAppArgsAfterDoubleDash | NAK-497 | planned |
| GoVersionDetectorTests.Detect_ReturnsDefault_WhenGoModAbsent | Detect_ReturnsDefault_WhenToolVersionsAbsent | NAK-490 | planned |
| GoVersionDetectorTests.Detect_ReadsGoDirective | Detect_ReadsToolVersionsEntry | NAK-490 | planned |
| GoVersionDetectorTests.Detect_ReadsGoDirectiveWithPatch | Detect_ReadsToolVersionsEntryWithPatch | NAK-490 | planned |
| GoVersionDetectorTests.Detect_PrefersToolchainOverGoDirective | Detect_PrefersToolVersionsOverrideOverToolVersionsEntry | NAK-490 | planned |
| GoVersionDetectorTests.Detect_FallsBackToGoDirective_WhenNoToolchain | Detect_FallsBackToToolVersionsEntry_WhenNoToolVersionsOverride | NAK-490 | planned |
| GoVersionDetectorTests.Detect_ReturnsDefault_WhenGoModHasNoRecognisedDirective | Detect_ReturnsDefault_WhenToolVersionsHasNoRecognisedEntry | NAK-490 | planned |

## Python

M1 source: `tests/Aspire.Hosting.Python.Tests`. Target: `tests/Aspire.Hosting.Elixir.Tests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AddPythonAppTests.AddPythonAppProducesDockerfileResourceInManifest | AddElixirAppProducesDockerfileResourceInManifest | NAK-498 | planned |
| AddPythonAppTests.AddInstrumentedPythonProjectProducesDockerfileResourceInManifest | AddInstrumentedElixirAppProducesDockerfileResourceInManifest | NAK-498 | planned |
| AddPythonAppTests.PythonResourceFinishesSuccessfully | ElixirAppResourceFinishesSuccessfully | NAK-489 | planned |
| AddPythonAppTests.PythonResourceSupportsWithReference | ElixirAppResourceSupportsWithReference | NAK-496 | planned |
| AddPythonAppTests.AddPythonApp_SetsResourcePropertiesCorrectly | AddElixirApp_SetsResourcePropertiesCorrectly | NAK-489 | planned |
| AddPythonAppTests.AddPythonApp_ObsoleteMethod_StillWorks | AddElixirApp_ObsoleteMethod_StillWorks | NAK-489 | planned |
| AddPythonAppTests.AddPythonAppWithScriptArgs_IncludesTheArguments | AddElixirAppWithScriptArgs_IncludesTheArguments | NAK-489 | planned |
| AddPythonAppTests.AddPythonApp_DoesNotThrowOnMissingVirtualEnvironment | AddElixirApp_DoesNotThrowOnMissingMixDeps | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_UpdatesCommandToUseNewVirtualEnvironment | WithMixDeps_UpdatesCommandToUseNewMixDeps | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_SupportsAbsolutePath | WithMixDeps_SupportsAbsolutePath | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_ThrowsOnNullBuilder | WithMixDeps_ThrowsOnNullBuilder | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_ThrowsOnNullOrEmptyPath | WithMixDeps_ThrowsOnNullOrEmptyPath | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_CanBeChainedWithOtherExtensions | WithMixDeps_CanBeChainedWithOtherExtensions | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_UsesAppDirectoryWhenVenvExistsThere | WithMixDeps_UsesAppDirectoryWhenMixDepsExistsThere | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_UsesAppHostDirectoryWhenVenvOnlyExistsThere | WithMixDeps_UsesAppHostDirectoryWhenMixDepsOnlyExistsThere | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_PrefersAppDirectoryWhenVenvExistsInBoth | WithMixDeps_PrefersAppDirectoryWhenMixDepsExistsInBoth | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_DefaultsToAppDirectoryWhenVenvExistsInNeither | WithMixDeps_DefaultsToAppDirectoryWhenMixDepsExistsInNeither | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_ExplicitPath_UsesVerbatim | WithMixDeps_ExplicitPath_UsesVerbatim | NAK-491 | planned |
| AddPythonAppTests.WithUv_CreatesUvEnvironmentResource | WithMixDeps_CreatesUvEnvironmentResource | NAK-491 | planned |
| AddPythonAppTests.WithUv_AddsUvSyncArgument | WithMixDeps_AddsUvSyncArgument | NAK-491 | planned |
| AddPythonAppTests.WithUv_AddsWaitForCompletionRelationship | WithMixDeps_AddsWaitForCompletionRelationship | NAK-491 | planned |
| AddPythonAppTests.WithUv_ThrowsOnNullBuilder | WithMixDeps_ThrowsOnNullBuilder | NAK-491 | planned |
| AddPythonAppTests.WithUv_IsIdempotent | WithMixDeps_IsIdempotent | NAK-491 | planned |
| AddPythonAppTests.InstallerResourceHasCertificateTrustScopeNone | InstallerResourceHasCertificateTrustScopeNone | NAK-489 | planned |
| AddPythonAppTests.WithPip_AfterWithUv_ReplacesPackageManager | WithMixDeps_AfterWithMixDeps_ReplacesPackageManager | NAK-491 | planned |
| AddPythonAppTests.WithUv_AfterWithPip_ReplacesPackageManager | WithMixDeps_AfterWithMixDeps_ReplacesPackageManager | NAK-491 | planned |
| AddPythonAppTests.AddPythonApp_CreatesResourceWithScriptEntrypoint | AddElixirApp_CreatesResourceWithScriptEntrypoint | NAK-489 | planned |
| AddPythonAppTests.AddPythonModule_CreatesResourceWithModuleEntrypoint | AddElixirApp_CreatesResourceWithScriptEntrypoint | NAK-489 | planned |
| AddPythonAppTests.AddPythonExecutable_CreatesResourceWithExecutableEntrypoint | AddElixirApp_CreatesResourceWithScriptEntrypoint | NAK-489 | planned |
| AddPythonAppTests.AddPythonApp_SetsCorrectCommandAndArguments | AddElixirApp_SetsCorrectCommandAndArguments | NAK-489 | planned |
| AddPythonAppTests.AddPythonModule_SetsCorrectCommandAndArguments | AddElixirApp_SetsCorrectCommandAndArguments | NAK-489 | planned |
| AddPythonAppTests.AddPythonExecutable_SetsCorrectCommandAndArguments | AddElixirApp_SetsCorrectCommandAndArguments | NAK-489 | planned |
| AddPythonAppTests.AddPythonModule_WithArgs_AddsArgumentsCorrectly | AddElixirApp_WithArgs_AddsArgumentsCorrectly | NAK-489 | planned |
| AddPythonAppTests.AddPythonApp_WithArgs_AddsArgumentsCorrectly | AddElixirApp_WithArgs_AddsArgumentsCorrectly | NAK-489 | planned |
| AddPythonAppTests.AddPythonExecutable_WithArgs_AddsArgumentsCorrectly | AddElixirApp_WithArgs_AddsArgumentsCorrectly | NAK-489 | planned |
| AddPythonAppTests.WithEntrypoint_ChangesEntrypointTypeAndValue | WithEntrypoint_ChangesEntrypointTypeAndValue | NAK-489 | planned |
| AddPythonAppTests.WithEntrypoint_UpdatesCommandForExecutableType | WithEntrypoint_UpdatesCommandForExecutableType | NAK-489 | planned |
| AddPythonAppTests.WithEntrypoint_ThrowsWhenVirtualEnvironmentNotFound | WithEntrypoint_ThrowsWhenMixDepsNotFound | NAK-491 | planned |
| AddPythonAppTests.WithEntrypoint_ThrowsOnNullBuilder | WithEntrypoint_ThrowsOnNullBuilder | NAK-489 | planned |
| AddPythonAppTests.WithEntrypoint_ThrowsOnNullOrEmptyEntrypoint | WithEntrypoint_ThrowsOnNullOrEmptyEntrypoint | NAK-489 | planned |
| AddPythonAppTests.WithUv_GeneratesDockerfileInPublishMode | WithMixDeps_GeneratesDockerfileInPublishMode | NAK-491 | planned |
| AddPythonAppTests.WithUv_GeneratesDockerfileInPublishMode_WithoutUvLock | WithMixDeps_GeneratesDockerfileInPublishMode_WithoutMixLock | NAK-491 | planned |
| AddPythonAppTests.WithDebugSupport_KeepsScriptArgumentInTheAppModelForScriptEntrypoint | WithElixirLsDebugSupport_KeepsScriptArgumentInTheAppModelForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_DoesntRemoveScriptArgumentForScriptEntrypoint_WhenResourceTypeNotSupported | WithElixirLsDebugSupport_DoesntRemoveScriptArgumentForScriptEntrypoint_WhenResourceTypeNotSupported | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_KeepsModuleArgumentsInTheAppModelForModuleEntrypoint | WithElixirLsDebugSupport_KeepsModuleArgumentsInTheAppModelForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_PopulatesWorkingDirectory_ForScriptEntrypoint | WithElixirLsDebugSupport_PopulatesWorkingDirectory_ForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_PopulatesWorkingDirectory_ForModuleEntrypoint | WithElixirLsDebugSupport_PopulatesWorkingDirectory_ForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_PopulatesWorkingDirectory_ForExecutableEntrypoint | WithElixirLsDebugSupport_PopulatesWorkingDirectory_ForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_PropagatesWorkingDirectoryOverride_ForExecutableEntrypoint | WithElixirLsDebugSupport_PropagatesWorkingDirectoryOverride_ForScriptEntrypoint | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_PropagatesWorkingDirectoryOverride | WithElixirLsDebugSupport_PropagatesWorkingDirectoryOverride | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_DoesntRemoveModuleArgumentsForModuleEntrypoint_WhenResourceTypeNotSupported | WithElixirLsDebugSupport_DoesntRemoveModuleArgumentsForScriptEntrypoint_WhenResourceTypeNotSupported | NAK-497 | planned |
| AddPythonAppTests.WithDebugSupport_ExecutableTypeDoesNotModifyArgs | WithElixirLsDebugSupport_ExecutableTypeDoesNotModifyArgs | NAK-497 | planned |
| AddPythonAppTests.PythonApp_SetsPythonUtf8EnvironmentVariable_OnWindowsInRunMode | n/a — PYTHONUTF8 is a CPython interpreter switch; the BEAM is UTF-8 by default and has no equivalent switch | NAK-492 | n/a |
| AddPythonAppTests.PythonApp_DoesNotSetPythonUtf8EnvironmentVariable_OnNonWindowsPlatforms | n/a — PYTHONUTF8 is a CPython interpreter switch; the BEAM is UTF-8 by default and has no equivalent switch | NAK-492 | n/a |
| AddPythonAppTests.PythonApp_DoesNotSetPythonUtf8EnvironmentVariable_InPublishMode | n/a — PYTHONUTF8 is a CPython interpreter switch; the BEAM is UTF-8 by default and has no equivalent switch | NAK-492 | n/a |
| AddPythonAppTests.WithUv_CustomBaseImages_GeneratesDockerfileWithCustomImages | WithMixDeps_CustomBaseImages_GeneratesDockerfileWithCustomImages | NAK-491 | planned |
| AddPythonAppTests.FallbackDockerfile_GeneratesDockerfileWithoutUv_WithRequirementsTxt | FallbackDockerfile_GeneratesDockerfileWithoutUv_WithMixLock | NAK-491 | planned |
| AddPythonAppTests.FallbackDockerfile_GeneratesDockerfileWithPyprojectToml | FallbackDockerfile_GeneratesDockerfileWithMixExs | NAK-498 | planned |
| AddPythonAppTests.FallbackDockerfile_GeneratesDockerfileWithoutAnyDependencyFiles | FallbackDockerfile_GeneratesDockerfileWithoutAnyDependencyFiles | NAK-498 | planned |
| AddPythonAppTests.FallbackDockerfile_GeneratesDockerfileForAllEntrypointTypes | FallbackDockerfile_GeneratesDockerfileForAllEntrypointTypes | NAK-498 | planned |
| AddPythonAppTests.AutoDetection_PyprojectToml_AddsPip | AutoDetection_MixExs_AddsPip | NAK-491 | planned |
| AddPythonAppTests.AutoDetection_RequirementsTxt_AddsPip | AutoDetection_MixLock_AddsPip | NAK-491 | planned |
| AddPythonAppTests.AutoDetection_PyprojectToml_TakesPrecedenceOverRequirementsTxt | AutoDetection_MixExs_TakesPrecedenceOverMixLock | NAK-491 | planned |
| AddPythonAppTests.AutoDetection_NoConfigFile_DoesNotAddPackageManager | AutoDetection_NoConfigFile_DoesNotAddPackageManager | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_DisableCreation_DoesNotCreateVenvCreator | WithMixDeps_DisableCreation_DoesNotCreateMixDepsCreator | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_EnableCreation_CreatesVenvCreator | WithMixDeps_EnableCreation_CreatesMixDepsCreator | NAK-491 | planned |
| AddPythonAppTests.WithVirtualEnvironment_DefaultBehavior_CreatesVenvCreator | WithMixDeps_DefaultBehavior_CreatesMixDepsCreator | NAK-491 | planned |
| AddPythonAppTests.WithUv_DisablesVenvCreation_And_SetsPackageManager | WithMixDeps_DisablesMixDepsCreation_And_SetsPackageManager | NAK-491 | planned |
| AddPythonAppTests.WithPip_CreatesDefaultVenv_And_WaitsForVenvCreation | WithMixDeps_CreatesDefaultMixDeps_And_WaitsForMixDepsCreation | NAK-491 | planned |
| AddPythonAppTests.WithPip_ThenWithVirtualEnvironment_CreateIfNotExistsTrue_CreatesVenv | WithMixDeps_ThenWithMixDeps_CreateIfNotExistsTrue_CreatesMixDeps | NAK-491 | planned |
| AddPythonAppTests.WithPip_ThenWithVirtualEnvironment_CreateIfNotExistsFalse_DoesNotCreateVenv | WithMixDeps_ThenWithMixDeps_CreateIfNotExistsFalse_DoesNotCreateMixDeps | NAK-491 | planned |
| AddPythonAppTests.MethodOrdering_WithPip_WithVirtualEnvironment_CreateTrue_WithPip_CreatesVenv | MethodOrdering_WithMixDeps_WithMixDeps_CreateTrue_WithMixDeps_CreatesMixDeps | NAK-491 | planned |
| AddPythonAppTests.MethodOrdering_WithPip_WithVirtualEnvironment_CreateFalse_WithPip_DoesNotCreateVenv | MethodOrdering_WithMixDeps_WithMixDeps_CreateFalse_WithMixDeps_DoesNotCreateMixDeps | NAK-491 | planned |
| AddPythonAppTests.MethodOrdering_WithPip_ThenWithUv_ReplacesPackageManager_And_DisablesVenvCreation | MethodOrdering_WithMixDeps_ThenWithMixDeps_ReplacesPackageManager_And_DisablesMixDepsCreation | NAK-491 | planned |
| AddPythonAppTests.MethodOrdering_WithUv_ThenWithPip_ReplacesPackageManager_And_EnablesVenvCreation | MethodOrdering_WithMixDeps_ThenWithMixDeps_ReplacesPackageManager_And_EnablesMixDepsCreation | NAK-491 | planned |
| AddPythonAppTests.WithPip_InstallFalse_CreatesInstallerWithExplicitStart | WithMixDeps_InstallFalse_CreatesInstallerWithExplicitStart | NAK-491 | planned |
| AddPythonAppTests.WithUv_InstallFalse_CreatesInstallerWithExplicitStart | WithMixDeps_InstallFalse_CreatesInstallerWithExplicitStart | NAK-491 | planned |
| AddPythonAppTests.InstallerResourceHasNameValidationPolicyAnnotation | InstallerResourceHasNameValidationPolicyAnnotation | NAK-489 | planned |
| AddPythonAppTests.VenvCreatorResourceHasNameValidationPolicyAnnotation | MixDepsCreatorResourceHasNameValidationPolicyAnnotation | NAK-491 | planned |
| AddUvicornAppTests.AddUvicornApp_CreatesUvicornAppResource | AddPhoenixApp_CreatesUvicornAppResource | NAK-493 | planned |
| AddUvicornAppTests.WithUv_GeneratesDockerfileInPublishMode | WithMixDeps_GeneratesDockerfileInPublishMode | NAK-491 | planned |
| PythonPublicApiTests.CtorPythonAppResourceShouldThrowWhenNameIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| PythonPublicApiTests.CtorPythonAppResourceShouldThrowWhenExecutablePathIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenExecutablePathIsNullOrEmpty | NAK-489 | planned |
| PythonPublicApiTests.CtorPythonAppResourceShouldThrowWhenAppDirectoryIsNull | CtorElixirAppResourceShouldThrowWhenAppDirectoryIsNull | NAK-489 | planned |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenBuilderIsNull | AddElixirAppShouldThrowWhenBuilderIsNull | NAK-489 | written |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenNameIsNullOrEmpty | AddElixirAppShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenAppDirectoryIsNull | AddElixirAppShouldThrowWhenAppDirectoryIsNull | NAK-489 | planned |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenScriptPathIsNullOrEmpty | AddElixirAppShouldThrowWhenScriptPathIsNullOrEmpty | NAK-489 | planned |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenScriptArgsIsNull | AddElixirAppShouldThrowWhenScriptArgsIsNull | NAK-489 | planned |
| PythonPublicApiTests.AddPythonAppShouldThrowWhenScriptArgsContainsIsNullOrEmpty | AddElixirAppShouldThrowWhenScriptArgsContainsIsNullOrEmpty | NAK-489 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenBuilderIsNull | AddElixirAppWithMixDepsPathShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenNameIsNullOrEmpty | AddElixirAppWithMixDepsPathShouldThrowWhenNameIsNullOrEmpty | NAK-491 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenAppDirectoryIsNull | AddElixirAppWithMixDepsPathShouldThrowWhenAppDirectoryIsNull | NAK-491 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptPathIsNullOrEmpty | AddElixirAppWithMixDepsPathShouldThrowWhenScriptPathIsNullOrEmpty | NAK-491 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenVirtualEnvironmentPathIsNull | AddElixirAppWithMixDepsPathShouldThrowWhenMixDepsPathIsNull | NAK-491 | planned |
| PythonPublicApiTests.AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptArgsIsNullOrEmpty | AddElixirAppWithMixDepsPathShouldThrowWhenScriptArgsIsNullOrEmpty | NAK-491 | planned |

## JavaScript

M1 source: `tests/Aspire.Hosting.JavaScript.Tests`. Target: `tests/Aspire.Hosting.Elixir.Tests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AddBunAppTests.VerifyManifest | VerifyManifest | NAK-489 | planned |
| AddBunAppTests.VerifyDockerfile | VerifyDockerfile | NAK-498 | planned |
| AddBunAppTests.VerifyDockerfileWithCustomBaseImage | VerifyDockerfileWithCustomBaseImage | NAK-498 | planned |
| AddBunAppTests.VerifyDockerfileEmitsPerDockerfileDockerignore | n/a — duplicated across every JavaScript resource-type test class as a Node/npm implementation detail; one Elixir Dockerfile/.dockerignore parity test covers it | NAK-498 | n/a |
| AddBunAppTests.VerifyDockerfileSkipsDockerignoreWhenUserAuthoredOneExists | VerifyDockerfileSkipsDockerignoreWhenUserAuthoredOneExists | NAK-498 | planned |
| AddBunAppTests.AddBunApp_DoesNotAddBunPackageManagerWhenNoPackageJson | AddElixirApp_DoesNotAddHexDepsWhenNoMixExs | NAK-491 | planned |
| AddBunAppTests.AddBunApp_AddsBunPackageManagerWhenPackageJsonExists | AddElixirApp_AddsBunPackageManagerWhenMixExsExists | NAK-491 | planned |
| AddBunAppTests.WithRunScript_SetsCustomRunCommand | WithMixTask_SetsCustomRunCommand | NAK-492 | planned |
| AddBunAppTests.AddBunApp_UsesBunCommand | AddElixirApp_UsesBunCommand | NAK-491 | planned |
| AddBunAppTests.AddBunApp_ThrowsForNullBuilder | AddElixirApp_ThrowsForNullBuilder | NAK-491 | planned |
| AddBunAppTests.AddBunApp_ThrowsForEmptyName | AddElixirApp_ThrowsForEmptyName | NAK-491 | planned |
| AddBunAppTests.AddBunApp_ThrowsForEmptyScriptPath | AddElixirApp_ThrowsForEmptyScriptPath | NAK-491 | planned |
| AddBunAppTests.AddBunApp_ConfiguresCertificateTrustForAppendScope | AddElixirApp_ConfiguresCertificateTrustForAppendScope | NAK-491 | planned |
| AddBunAppTests.AddBunApp_ConfiguresCertificateTrustForOverrideScope | AddElixirApp_ConfiguresCertificateTrustForOverrideScope | NAK-491 | planned |
| AddBunAppTests.BunApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | ElixirApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | NAK-497 | planned |
| AddBunAppTests.BunApp_WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode | ElixirApp_WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode | NAK-497 | planned |
| AddBunAppTests.BunApp_WithRunScript_AddsSupportsDebuggingAnnotation | ElixirApp_WithMixTask_AddsSupportsDebuggingAnnotation | NAK-491 | planned |
| AddBunAppTests.BunApp_WithPackageJson_HasPackageManagerAnnotation | ElixirApp_WithMixExs_HasPackageManagerAnnotation | NAK-491 | planned |
| AddBunAppTests.BunApp_DirectFile_ProducesBunRuntimeExecutable | ElixirApp_DirectFile_ProducesBunRuntimeExecutable | NAK-491 | planned |
| AddBunAppTests.BunApp_WithRunScriptAndPackageManager_ProducesBunRuntimeExecutable | ElixirApp_WithMixTaskAndPackageManager_ProducesBunRuntimeExecutable | NAK-491 | planned |
| AddJavaScriptAppTests.VerifyDockerfile | VerifyDockerfile | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyDockerfileWhenPublishedAsStaticWebsiteWithoutSpaFallback | VerifyDockerfileWhenPublishedAsStaticWebsiteWithoutSpaFallback | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyDockerfileWhenPublishedAsNodeServer | VerifyDockerfileWhenPublishedAsNodeServer | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyDockerfileWhenPublishedAsPackageScript | VerifyDockerfileWhenPublishedAsPackageScript | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfile | VerifyPnpmDockerfile | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileUsesBootstrapRegistryOnlyForNpm | VerifyPnpmDockerfileUsesBootstrapRegistryOnlyForNpm | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileWhenPublishedAsPackageScript | VerifyPnpmDockerfileWhenPublishedAsPackageScript | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileThrowsWhenRunScriptNameIsExplicit | PublishWithExistingDockerfileThrowsWhenMixTaskNameIsExplicit | NAK-498 | planned |
| AddJavaScriptAppTests.PublishModelWithExistingDockerfileThrowsWhenRunScriptNameIsExplicit | PublishModelWithExistingDockerfileThrowsWhenMixTaskNameIsExplicit | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileThrowsWhenWithRunScriptOverridesDefault | PublishWithExistingDockerfileThrowsWhenWithMixTaskOverridesDefault | NAK-498 | planned |
| AddJavaScriptAppTests.PublishPipelineWithExistingDockerfileThrowsFromValidationStepWhenRunScriptNameIsExplicit | PublishPipelineWithExistingDockerfileThrowsFromValidationStepWhenMixTaskNameIsExplicit | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileAllowsImplicitDefaultRunScript | PublishWithExistingDockerfileAllowsImplicitDefaultMixTask | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileAllowsExplicitEntrypointOverride | PublishWithExistingDockerfileAllowsExplicitEntrypointOverride | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileAllowsWithRunScriptMatchingDefault | PublishWithExistingDockerfileAllowsWithMixTaskMatchingDefault | NAK-498 | planned |
| AddJavaScriptAppTests.PublishWithExistingDockerfileThrowsAndIncludesArgsWhenDefaultScriptHasArgs | PublishWithExistingDockerfileThrowsAndIncludesArgsWhenDefaultScriptHasArgs | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileCopiesWorkspaceFileBeforeInstall | VerifyPnpmDockerfileCopiesWorkspaceFileBeforeInstall | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileUsesPackageManagerVersion | VerifyPnpmDockerfileUsesPackageManagerVersion | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileUsesNodeForPackageManagerIntegrity | n/a — package.json's packageManager field with a pinned corepack hash is npm-ecosystem specific; mix has one canonical toolchain resolved via .tool-versions | NAK-498 | n/a |
| AddJavaScriptAppTests.VerifyPnpmDockerfileNormalizesPackageManagerIntegrityHash | n/a — package.json's packageManager field with a pinned corepack hash is npm-ecosystem specific; mix has one canonical toolchain resolved via .tool-versions | NAK-498 | n/a |
| AddJavaScriptAppTests.VerifyPnpmDockerfileUsesValidPackageManagerVersion | n/a — package.json's packageManager field with a pinned corepack hash is npm-ecosystem specific; mix has one canonical toolchain resolved via .tool-versions | NAK-498 | n/a |
| AddJavaScriptAppTests.VerifyPnpmRejectsInvalidPackageManagerSpecification | n/a — package.json's packageManager field with a pinned corepack hash is npm-ecosystem specific; mix has one canonical toolchain resolved via .tool-versions | NAK-491 | n/a |
| AddJavaScriptAppTests.VerifyPnpmDockerfileBuildSucceeds | VerifyPnpmDockerfileBuildSucceeds | NAK-498 | planned |
| AddJavaScriptAppTests.VerifyPnpmDockerfileWhenPublishedAsPackageScriptRunsWithoutNetwork | VerifyPnpmDockerfileWhenPublishedAsPackageScriptRunsWithoutNetwork | NAK-498 | planned |
| AddNodeAppTests.VerifyManifest | VerifyManifest | NAK-489 | planned |
| AddNodeAppTests.VerifyDockerfile | VerifyDockerfile | NAK-498 | planned |
| AddNodeAppTests.VerifyDockerfileWithBuildScript | VerifyDockerfileWithMixCompile | NAK-498 | planned |
| AddNodeAppTests.VerifyDockerfileWithCustomBaseImage | VerifyDockerfileWithCustomBaseImage | NAK-498 | planned |
| AddNodeAppTests.AddNodeApp_DoesNotAddNpmWhenNoPackageJson | AddElixirApp_DoesNotAddHexDepsWhenNoMixExs | NAK-491 | planned |
| AddNodeAppTests.AddNodeApp_AddsNpmWhenPackageJsonExists | AddElixirApp_AddsNpmWhenMixExsExists | NAK-491 | planned |
| AddNodeAppTests.WithRunScript_SetsCustomRunCommand | WithMixTask_SetsCustomRunCommand | NAK-492 | planned |
| AddNodeAppTests.VerifyNodeAppWithContainerFilesGeneratesCorrectDockerfile | VerifyElixirAppWithContainerFilesGeneratesCorrectDockerfile | NAK-498 | planned |
| AddNodeAppTests.VerifyNodeAppWithContainerFilesFromResourceWithDashesGeneratesCorrectDockerfile | VerifyElixirAppWithContainerFilesFromResourceWithDashesGeneratesCorrectDockerfile | NAK-498 | planned |
| MyFilesContainer.NodeApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | ElixirApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | NAK-497 | planned |
| MyFilesContainer.NodeApp_WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode | ElixirApp_WithVSCodeDebugging_DoesNotAddAnnotationInPublishMode | NAK-497 | planned |
| MyFilesContainer.ViteApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | PhoenixApp_WithVSCodeDebugging_AddsSupportsDebuggingAnnotation | NAK-497 | planned |
| MyFilesContainer.ViteApp_WithBrowserDebugger_CreatesChildResource | PhoenixApp_WithBrowserDebugger_CreatesChildResource | NAK-497 | planned |
| MyFilesContainer.ViteApp_WithBrowserDebugger_DefaultsToEdgeBrowser | PhoenixApp_WithBrowserDebugger_DefaultsToEdgeBrowser | NAK-497 | planned |
| MyFilesContainer.ViteApp_WithBrowserDebugger_UsesSpecifiedBrowser | PhoenixApp_WithBrowserDebugger_UsesSpecifiedBrowser | NAK-497 | planned |
| MyFilesContainer.ViteApp_WithBrowserDebugger_WithoutEndpoint_DeferredValidation | PhoenixApp_WithBrowserDebugger_WithoutEndpoint_DeferredValidation | NAK-497 | planned |
| MyFilesContainer.WithReferenceDispatchesNodeAppServiceReference | WithReferenceDispatchesElixirAppServiceReference | NAK-496 | planned |
| MyFilesContainer.NodeApp_DirectFile_ProducesNodeRuntimeExecutable | ElixirApp_DirectFile_ProducesNodeRuntimeExecutable | NAK-489 | planned |
| MyFilesContainer.ViteApp_DevServer_ProducesPackageManagerRuntimeExecutable | PhoenixApp_DevServer_ProducesPackageManagerRuntimeExecutable | NAK-493 | planned |
| AddViteAppTests.VerifyDefaultDockerfile | VerifyDefaultDockerfile | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPublishedAsStaticWebsite | VerifyDockerfileWhenPublishedAsStaticWebsite | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPublishedAsStaticWebsiteWithApiProxy | VerifyDockerfileWhenPublishedAsStaticWebsiteWithApiProxy | NAK-498 | planned |
| AddViteAppTests.PublishAsStaticWebsiteSetsYarpEnvironmentVariables | PublishAsPhoenixStaticAssetsSetsReverseProxyEnvironmentVariables | NAK-498 | planned |
| AddViteAppTests.PublishAsStaticWebsiteWithApiProxySetsReverseProxyEnvironmentVariables | PublishAsPhoenixStaticAssetsWithApiProxySetsReverseProxyEnvironmentVariables | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPublishedAsStaticWebsiteWithCustomOutputPath | VerifyDockerfileWhenPublishedAsStaticWebsiteWithCustomOutputPath | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPublishedAsNodeServer | VerifyDockerfileWhenPublishedAsNodeServer | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPublishedAsNextStandalone | VerifyDockerfileWhenPublishedAsPhoenixRelease | NAK-499 | planned |
| AddViteAppTests.VerifyDockerfileWhenNextJsAppUsesPnpm | VerifyDockerfileWhenPhoenixAppUsesPnpm | NAK-499 | planned |
| AddViteAppTests.VerifyDockerfileWhenPackageScriptUsesPnpm | VerifyDockerfileWhenPackageScriptUsesPnpm | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWhenPackageScriptUsesBun | VerifyDockerfileWhenPackageScriptUsesBun | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWithNodeVersionFromNvmrc | VerifyDockerfileWithElixirVersionFromToolVersions | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWithNodeVersionFromNodeVersion | VerifyDockerfileWithElixirVersionFromToolVersions | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWithNodeVersionFromToolVersions | VerifyDockerfileWithElixirVersionFromToolVersions | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileWithNodeVersionFromToolVersionsUsingTabs | VerifyDockerfileWithElixirVersionFromToolVersionsUsingTabs | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileIgnoresPackageJsonEnginesWhenNoPinnedVersionExists | VerifyDockerfileIgnoresMixExsEnginesWhenNoPinnedVersionExists | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileDefaultsTo22WhenNoVersionFound | VerifyDockerfileDefaultsToLatestWhenNoVersionFound | NAK-498 | planned |
| AddViteAppTests.VerifyDockerfileHandlesVariousVersionFormats | VerifyDockerfileHandlesVariousVersionFormats | NAK-498 | planned |
| AddViteAppTests.VerifyCustomBaseImage | VerifyCustomBaseImage | NAK-489 | planned |
| AddViteAppTests.AddViteApp_WithViteConfigPath_AppliesConfigArgument | AddPhoenixApp_WithViteConfigPath_AppliesConfigArgument | NAK-493 | planned |
| AddViteAppTests.AddViteApp_WithoutViteConfigPath_DoesNotApplyConfigArgument | AddPhoenixApp_WithoutViteConfigPath_DoesNotApplyConfigArgument | NAK-493 | planned |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WithExistingConfigArgument_ReplacesConfigPath | AddPhoenixApp_CertificateTrustConfig_WithExistingConfigArgument_ReplacesConfigPath | NAK-495 | planned |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WithoutExistingConfigArgument_DetectsDefaultConfig | AddPhoenixApp_CertificateTrustConfig_WithoutExistingConfigArgument_DetectsDefaultConfig | NAK-495 | planned |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WithMissingConfigFile_DoesNotAddConfigArgument | AddPhoenixApp_CertificateTrustConfig_WithMissingConfigFile_DoesNotAddConfigArgument | NAK-495 | planned |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WithMissingNodeModules_PreservesConfigArgument | n/a — the wrapper-script mechanism resolves against node_modules; mix has no node_modules-style local module cache to write a wrapper into | NAK-495 | n/a |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WithPassword_SetsPasswordEnvironmentVariable | AddPhoenixApp_CertificateTrustConfig_WithPassword_SetsPasswordEnvironmentVariable | NAK-495 | planned |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_WritesWrapperToNearestNodeModules | n/a — the wrapper-script mechanism resolves against node_modules; mix has no node_modules-style local module cache to write a wrapper into | NAK-495 | n/a |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_SharedNodeModules_WritesResourceSpecificWrappers | n/a — the wrapper-script mechanism resolves against node_modules; mix has no node_modules-style local module cache to write a wrapper into | NAK-495 | n/a |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_SharedNodeModules_WritesAppHostSpecificWrappers | n/a — the wrapper-script mechanism resolves against node_modules; mix has no node_modules-style local module cache to write a wrapper into | NAK-495 | n/a |
| AddViteAppTests.AddViteApp_ServerAuthCertConfig_DetectsAllDefaultConfigFileFormats | AddPhoenixApp_CertificateTrustConfig_DetectsAllDefaultConfigFileFormats | NAK-495 | planned |
| AddViteAppTests.NextJsAppHasBuildValidationStep | PhoenixAppHasCompileValidationStep | NAK-492 | planned |
| AddViteAppTests.DisableBuildValidationAddsSuppressAnnotation | DisableCompileValidationAddsSuppressAnnotation | NAK-492 | planned |
| AddViteAppTests.NextJsStandaloneCheckFailsInPipelineWhenMissing | PhoenixStandaloneCheckFailsInPipelineWhenMissing | NAK-489 | planned |
| AddViteAppWithPnpmTests.AddViteApp_WithPnpm_DoesNotIncludeSeparator | AddPhoenixApp_WithMixDeps_DoesNotIncludeSeparator | NAK-493 | planned |
| AddViteAppWithPnpmTests.AddViteApp_WithBun_DoesNotIncludeSeparator | AddPhoenixApp_WithMixDeps_DoesNotIncludeSeparator | NAK-493 | planned |
| AddViteAppWithPnpmTests.AddViteApp_WithNpm_IncludesSeparator | AddPhoenixApp_WithMixDeps_IncludesSeparator | NAK-493 | planned |
| AddViteAppWithPnpmTests.AddViteApp_WithYarn_IncludesSeparator | AddPhoenixApp_WithMixDeps_IncludesSeparator | NAK-493 | planned |
| BunFunctionalTests.VerifyBunAppDirectExecutionWorks | VerifyElixirAppDirectExecutionWorks | NAK-491 | planned |
| BunFunctionalTests.VerifyBunAppPackageScriptWorks | VerifyElixirAppPackageScriptWorks | NAK-491 | planned |
| IntegrationTests.ResourceBasedPackageInstallersAppearInApplicationModel | ResourceBasedPackageInstallersAppearInApplicationModel | NAK-491 | planned |
| IntegrationTests.InstallerResourcesHaveCorrectExecutableConfiguration | InstallerResourcesHaveCorrectExecutableConfiguration | NAK-491 | planned |
| NodeFunctionalTests.VerifyNodeAppWorks | VerifyElixirAppWorks | NAK-489 | planned |
| NodeFunctionalTests.VerifyNpmAppWorks | VerifyNpmAppWorks | NAK-491 | planned |
| NodeJsPublicApiTests.CtorNodeAppResourceShouldThrowWhenNameIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| NodeJsPublicApiTests.CtorNodeAppResourceShouldThrowWhenCommandIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenCommandIsNullOrEmpty | NAK-489 | planned |
| NodeJsPublicApiTests.CtorNodeAppResourceShouldThrowWhenWorkingDirectoryIsNull | CtorElixirAppResourceShouldThrowWhenWorkingDirectoryIsNull | NAK-489 | written |
| NodeJsPublicApiTests.AddNodeAppShouldThrowWhenBuilderIsNull | AddElixirAppShouldThrowWhenBuilderIsNull | NAK-489 | written |
| NodeJsPublicApiTests.AddNodeAppShouldThrowWhenNameIsNullOrEmpty | AddElixirAppShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| NodeJsPublicApiTests.AddNodeAppShouldThrowWhenScriptPathIsNullOrEmpty | AddElixirAppShouldThrowWhenScriptPathIsNullOrEmpty | NAK-489 | planned |
| NodeJsPublicApiTests.AddJavaScriptAppShouldThrowWhenBuilderIsNull | AddElixirAppShouldThrowWhenBuilderIsNull | NAK-489 | written |
| NodeJsPublicApiTests.AddJavaScriptAppShouldThrowWhenNameIsNullOrEmpty | AddElixirAppShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |
| NodeJsPublicApiTests.AddJavaScriptAppShouldThrowWhenWorkingDirectoryIsNull | AddElixirAppShouldThrowWhenWorkingDirectoryIsNull | NAK-489 | planned |
| NodeJsPublicApiTests.AddJavaScriptAppShouldThrowWhenScriptNameIsNullOrEmpty | AddElixirAppShouldThrowWhenScriptNameIsNullOrEmpty | NAK-489 | planned |
| NodeJsPublicApiTests.PublishAsStaticWebsiteShouldThrowWhenBuilderIsNull | PublishAsPhoenixStaticAssetsShouldThrowWhenBuilderIsNull | NAK-498 | planned |
| NodeJsPublicApiTests.PublishAsNodeServerShouldThrowWhenBuilderIsNull | PublishAsElixirReleaseShouldThrowWhenBuilderIsNull | NAK-498 | planned |
| NodeJsPublicApiTests.PublishAsNodeServerShouldThrowWhenEntryPointIsNullOrEmpty | PublishAsElixirReleaseShouldThrowWhenEntryPointIsNullOrEmpty | NAK-498 | planned |
| NodeJsPublicApiTests.PublishAsNodeServerShouldThrowWhenOutputPathIsNullOrEmpty | PublishAsElixirReleaseShouldThrowWhenOutputPathIsNullOrEmpty | NAK-498 | planned |
| NodeJsPublicApiTests.PublishAsPackageScriptShouldThrowWhenBuilderIsNull | PublishAsMixTaskShouldThrowWhenBuilderIsNull | NAK-498 | planned |
| NodeJsPublicApiTests.PublishAsPackageScriptShouldThrowWhenScriptNameIsNullOrEmpty | PublishAsMixTaskShouldThrowWhenScriptNameIsNullOrEmpty | NAK-498 | planned |
| NodeJsPublicApiTests.AddNextJsAppShouldThrowWhenBuilderIsNull | AddPhoenixAppShouldThrowWhenBuilderIsNull | NAK-489 | planned |
| PackageInstallationTests.WithNpm_CanBeConfiguredWithInstall | WithMixDeps_CanBeConfiguredWithInstall | NAK-491 | planned |
| PackageInstallationTests.WithNpm_ExcludedFromPublishMode | WithMixDeps_ExcludedFromPublishMode | NAK-498 | planned |
| PackageInstallationTests.WithYarn_CreatesInstallerWhenInstallIsTrue | WithMixDeps_CreatesInstallerWhenInstallIsTrue | NAK-491 | planned |
| PackageInstallationTests.WithYarn_DoesNotCreateInstallerWhenInstallIsFalse | WithMixDeps_DoesNotCreateInstallerWhenInstallIsFalse | NAK-491 | planned |
| PackageInstallationTests.WithPnpm_CreatesInstallerWhenInstallIsTrue | WithMixDeps_CreatesInstallerWhenInstallIsTrue | NAK-491 | planned |
| PackageInstallationTests.WithPnpm_DoesNotCreateInstallerWhenInstallIsFalse | WithMixDeps_DoesNotCreateInstallerWhenInstallIsFalse | NAK-491 | planned |
| PackageInstallationTests.WithNpm_CreatesInstallerWithCustomCommand | WithMixDeps_CreatesInstallerWithCustomCommand | NAK-491 | planned |
| PackageInstallationTests.WithBuildScript_SetsCustomBuildCommand | WithMixCompile_SetsCustomBuildCommand | NAK-492 | planned |
| PackageInstallationTests.WithRunScript_SetsCustomRunCommand | WithMixTask_SetsCustomRunCommand | NAK-492 | planned |
| PackageInstallationTests.WithNpmInstallWithYarnNoInstall | WithMixDepsInstallWithMixDepsNoInstall | NAK-491 | planned |
| PackageInstallationTests.WithNpmNoInstallWithYarnInstall | WithMixDepsNoInstallWithMixDepsInstall | NAK-491 | planned |
| PackageInstallationTests.WithNpmInstallWithYarnInstall | WithMixDepsInstallWithMixDepsInstall | NAK-491 | planned |
| PackageInstallationTests.WithNpm_DefaultInstallsPackages | WithMixDeps_DefaultInstallsPackages | NAK-491 | planned |
| PackageInstallationTests.WithYarn_DefaultInstallsPackages | WithMixDeps_DefaultInstallsPackages | NAK-491 | planned |
| PackageInstallationTests.WithPnpm_DefaultInstallsPackages | WithMixDeps_DefaultInstallsPackages | NAK-491 | planned |
| PackageInstallationTests.AddViteApp_DefaultInstallsPackages | AddPhoenixApp_DefaultInstallsPackages | NAK-493 | planned |
| PackageInstallationTests.WithNpm_DefaultsArgsInPublishMode | WithMixDeps_DefaultsArgsInPublishMode | NAK-498 | planned |
| PackageInstallationTests.WithNpm_CanChangeInstallCommandAndArgs | WithMixDeps_CanChangeInstallCommandAndArgs | NAK-491 | planned |
| PackageInstallationTests.WithYarn_DefaultsArgsInPublishMode | WithMixDeps_DefaultsArgsInPublishMode | NAK-498 | planned |
| PackageInstallationTests.WithYarn_ReturnsImmutable_WhenYarnRcYmlExists | n/a — Yarn's .yarnrc.yml / PnP lockfile detection is Yarn-specific; mix.lock has no comparable multi-mode lockfile format | NAK-491 | n/a |
| PackageInstallationTests.WithYarn_ReturnsImmutable_WhenYarnReleasesDirExists | n/a — Yarn's .yarn/releases directory is Yarn-specific; mix has no bundled per-project tool release cache | NAK-491 | n/a |
| PackageInstallationTests.WithPnpm_DefaultsArgsInPublishMode | WithMixDeps_DefaultsArgsInPublishMode | NAK-498 | planned |
| PackageInstallationTests.WithBun_DefaultsArgsInPublishMode | WithMixDeps_DefaultsArgsInPublishMode | NAK-498 | planned |
| PackageInstallationTests.InstallerResourceHasNameValidationPolicyAnnotation | InstallerResourceHasNameValidationPolicyAnnotation | NAK-491 | planned |
| RequiredCommandTests.AddNodeApp_DefaultsToNode | AddElixirApp_DefaultsToNode | NAK-489 | planned |
| RequiredCommandTests.AddNodeApp_WithBun_RequiresNodeAndBun | AddElixirApp_WithMixDeps_RequiresMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.AddNodeApp_WithNpm_RequiresNodeAndNpm | AddElixirApp_WithMixDeps_RequiresMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.AddNodeApp_WithRunScript_WithBun_RequiresOnlyBun | AddElixirApp_WithMixTask_WithMixDeps_RequiresOnlyMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.AddViteApp_DefaultsToNodeAndNpm | AddPhoenixApp_DefaultsToNodeAndNpm | NAK-493 | planned |
| RequiredCommandTests.AddViteApp_WithBun_RequiresOnlyBun | AddPhoenixApp_WithMixDeps_RequiresOnlyMixAndElixir | NAK-493 | planned |
| RequiredCommandTests.AddViteApp_WithNpm_RequiresNodeAndNpm | AddPhoenixApp_WithMixDeps_RequiresMixAndElixir | NAK-493 | planned |
| RequiredCommandTests.AddViteApp_WithYarn_RequiresNodeAndYarn | AddPhoenixApp_WithMixDeps_RequiresMixAndElixir | NAK-493 | planned |
| RequiredCommandTests.AddViteApp_WithPnpm_RequiresNodeAndPnpm | AddPhoenixApp_WithMixDeps_RequiresMixAndElixir | NAK-493 | planned |
| RequiredCommandTests.AddJavaScriptApp_WithBun_RequiresOnlyBun | AddElixirApp_WithMixDeps_RequiresOnlyMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.AddBunApp_RequiresOnlyBun | AddElixirApp_RequiresOnlyMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.AddBunApp_WithPackageJson_RequiresOnlyBunWithoutDuplicates | AddElixirApp_WithMixExs_RequiresOnlyMixAndElixirWithoutDuplicates | NAK-491 | planned |
| RequiredCommandTests.AddBunApp_WithNpm_RequiresBunNodeAndNpm | AddElixirApp_WithMixDeps_RequiresMixAndElixir | NAK-491 | planned |
| RequiredCommandTests.WithBun_ThenWithNpm_LastSelectionWins | WithMixDeps_ThenWithMixDeps_LastSelectionWins | NAK-491 | planned |
| RequiredCommandTests.WithNpm_ThenWithBun_LastSelectionWins | WithMixDeps_ThenWithMixDeps_LastSelectionWins | NAK-491 | planned |
| ResourceCreationTests.DefaultViteAppUsesNpm | DefaultPhoenixAppUsesNpm | NAK-493 | planned |
| ResourceCreationTests.ViteAppUsesSpecifiedWorkingDirectory | PhoenixAppUsesSpecifiedWorkingDirectory | NAK-493 | planned |
| ResourceCreationTests.ViteAppHasExposedHttpEndpoints | PhoenixAppHasExposedHttpEndpoints | NAK-493 | planned |
| ResourceCreationTests.ViteAppDoesNotExposeExternalHttpEndpointsByDefault | PhoenixAppDoesNotExposeExternalHttpEndpointsByDefault | NAK-493 | planned |
| ResourceCreationTests.WithNpmDefaultsToInstallCommand | WithMixDepsDefaultsToInstallCommand | NAK-491 | planned |
| ResourceCreationTests.ViteAppConfiguresPortFromEnvironment | PhoenixAppConfiguresPortFromEnvironment | NAK-493 | planned |
| ResourceCreationTests.WithNpmInstallFalseDoesNotCreateInstaller | WithMixDepsInstallFalseDoesNotCreateInstaller | NAK-491 | planned |
| ResourceCreationTests.InstallerResourceHasCertificateTrustScopeNone | InstallerResourceHasCertificateTrustScopeNone | NAK-491 | planned |

## Java

M1 source: `tests/Aspire.Hosting.Java.Tests`. Target: `tests/Aspire.Hosting.Elixir.Tests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AddJavaAppPublishTests.VerifyPublish_GeneratesAMavenBuildAndJreRuntimePair | VerifyPublish_GeneratesAMavenBuildAndJreRuntimePair | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_TheJarSelectionCopiesAnArtifactWhoseNameContainsWhitespace | VerifyPublish_TheReleaseSelectionCopiesAnArtifactWhoseNameContainsWhitespace | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_GeneratesAGradleBuild | VerifyPublish_GeneratesAGradleBuild | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_CopiesABuildProducedOtelAgentIntoTheRuntimeImage | VerifyPublish_CopiesABuildProducedOtlpExporterIntoTheRuntimeImage | NAK-495 | planned |
| AddJavaAppPublishTests.VerifyPublish_WithJvmArgs_ComposesWithBuildProducedOtelAgent | VerifyPublish_WithMixEnv_ComposesWithBuildProducedOtlpExporter | NAK-495 | planned |
| AddJavaAppPublishTests.VerifyPublish_StripsExactlyOneLeadingDotSlashFromTheOtelAgentPath | VerifyPublish_StripsExactlyOneLeadingDotSlashFromTheOtlpExporterPath | NAK-495 | planned |
| AddJavaAppPublishTests.AnOtelAgentPathOutsideTheBuildContextIsRejected | AnOtlpExporterPathOutsideTheBuildContextIsRejected | NAK-495 | planned |
| AddJavaAppPublishTests.VerifyPublish_DoesNotCopyAnAbsoluteOtelAgentPath | VerifyPublish_DoesNotCopyAnAbsoluteOtlpExporterPath | NAK-495 | planned |
| AddJavaAppPublishTests.VerifyPublish_AWindowsAbsoluteOtelAgentIsRejectedOnEveryPlatform | n/a — tests Windows-specific path or batch-wrapper handling that the Elixir integration does not carry (mix paths are POSIX-normalized by BEAM tooling on every OS) | NAK-495 | n/a |
| AddJavaAppPublishTests.VerifyPublish_UsesTheWrapperWhenTheProjectShipsOne | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingAProjectWithoutAWrapperIsRejectedRatherThanUsingTheImageTool | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_ReusesTheArgumentsConfiguredForTheHostBuildStep | VerifyPublish_ReusesTheArgumentsConfiguredForTheHostBuildStep | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_DetectsTheBuildToolFromDiskWhenOnlyAJarPathWasGiven | VerifyPublish_DetectsTheBuildToolFromDiskWhenOnlyAReleasePathWasGiven | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAJarPathThatEscapesTheBuildDirectoryIsRejectedRatherThanGlobbed | PublishingAReleasePathThatEscapesTheBuildDirectoryIsRejectedRatherThanGlobbed | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAJarPathContainingWhitespaceQuotesItRatherThanFallingBackToTheGlob | PublishingAReleasePathContainingWhitespaceQuotesItRatherThanFallingBackToTheGlob | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_CopiesTheExplicitArtifactWhenWithJarArtifactIsUsed | VerifyPublish_CopiesTheExplicitArtifactWhenWithMixReleaseArtifactIsUsed | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_FailsTheContainerBuildWhenTheJarIsAmbiguous | VerifyPublish_FailsTheContainerBuildWhenTheReleaseIsAmbiguous | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_HonoursWithDockerfileBaseImage | VerifyPublish_HonoursWithDockerfileBaseImage | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_RunsAsANonRootUser | VerifyPublish_RunsAsANonRootUser | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_UsesTheExecFormEntrypointSoTheJvmReceivesSigterm | VerifyPublish_UsesTheExecFormEntrypointSoTheJvmReceivesSigterm | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_EmitsABuildContextIgnoreThatExcludesBuildOutputDirectories | VerifyPublish_EmitsABuildContextIgnoreThatExcludesBuildOutputDirectories | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_LeavesAnAuthoredDockerignoreAlone | VerifyPublish_LeavesAnAuthoredDockerignoreAlone | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_LeavesAnAuthoredDockerfileAlone | VerifyPublish_LeavesAnAuthoredDockerfileAlone | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAnAuthoredDockerfileRejectsABuildProducedAgent | PublishingAnAuthoredDockerfileRejectsABuildProducedAgent | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAnAuthoredDockerfileAcceptsAnAbsoluteAgentPath | PublishingAnAuthoredDockerfileAcceptsAnAbsoluteAgentPath | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_ProducesAContainerManifestEntry | VerifyPublish_ProducesAContainerManifestEntry | NAK-498 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_ReadsTheTargetReleaseFromAPom | ElixirVersionDetector_ReadsTheTargetReleaseFromAPom | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_SkipsAPropertyReferenceInFavourOfALaterLiteral | ElixirVersionDetector_SkipsAPropertyReferenceInFavourOfALaterLiteral | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_IgnoresATargetOutsideAPluginConfiguration | ElixirVersionDetector_IgnoresATargetOutsideAPluginConfiguration | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_ReadsTheTargetReleaseFromAGradleBuildScript | ElixirVersionDetector_ReadsTheTargetReleaseFromAGradleBuildScript | NAK-491 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_PrefersThePomWhenBothBuildFilesArePresent | ElixirVersionDetector_PrefersThePomWhenBothBuildFilesArePresent | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_FollowsTheResolvedBuildToolWhenBothBuildFilesArePresent | ElixirVersionDetector_FollowsTheResolvedBuildToolWhenBothBuildFilesArePresent | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_FallsBackToTheOtherBuildFileWhenTheResolvedToolDeclaresNothing | ElixirVersionDetector_FallsBackToTheOtherBuildFileWhenTheResolvedToolDeclaresNothing | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_IgnoresReleaseAndTargetOutsideTheCompilerPlugin | ElixirVersionDetector_IgnoresReleaseAndTargetOutsideTheCompilerPlugin | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_ReadsAnExecutionLevelCompilerConfiguration | ElixirVersionDetector_ReadsAnExecutionLevelCompilerConfiguration | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_FallsBackWhenOnlyAnUnrelatedPluginDeclaresATarget | ElixirVersionDetector_FallsBackWhenOnlyAnUnrelatedPluginDeclaresATarget | NAK-490 | planned |
| AddJavaAppPublishTests.JavaVersionDetector_FallsBackWhenThePomCannotBeParsed | ElixirVersionDetector_FallsBackWhenThePomCannotBeParsed | NAK-490 | planned |
| AddJavaAppPublishTests.ResolveBuildTool_ThrowsWhenNoBuildToolCanBeFound | ResolveToolVersions_ThrowsWhenNoBuildToolCanBeFound | NAK-491 | planned |
| AddJavaAppPublishTests.ResolveBuildTool_DetectsGradleFromAnyOfItsBuildFiles | ResolveToolVersions_DetectsGradleFromAnyOfItsBuildFiles | NAK-491 | planned |
| AddJavaAppPublishTests.ResolveBuildTool_PrefersTheConfiguredBuildStepOverWhatIsOnDisk | ResolveToolVersions_PrefersTheConfiguredBuildStepOverWhatIsOnDisk | NAK-491 | planned |
| AddJavaAppPublishTests.ApplicationArgumentsSurvivePublishingWhileLaunchToolArgumentsDoNot | ApplicationArgumentsSurvivePublishingWhileLaunchToolArgumentsDoNot | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingWithoutAWrapperIsRejected | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingWithAWrapperOutsideTheBuildContextIsRejected | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingAJarOutsideTheBuildContextIsRejected | PublishingAJarOutsideTheBuildContextIsRejected | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAJarArtifactOutsideTheBuildContextIsRejected | PublishingAJarArtifactOutsideTheBuildContextIsRejected | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAJarInsideTheBuildContextKeepsItsContextRelativePath | PublishingAJarInsideTheBuildContextKeepsItsContextRelativePath | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAJarPathPrefixedWithDotSlashStillResolvesInsideTheContext | PublishingAReleasePathPrefixedWithDotSlashStillResolvesInsideTheContext | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingWithAWindowsBatchWrapperUsesThePosixSibling | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingWithAWindowsBatchWrapperAndNoPosixSiblingIsRejected | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingRejectsAWrapperWithoutItsPropertiesFile | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingHonoursAWrapperSelectedWithWithWrapperPath | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_WithAPrebuiltJarAndNoBuildTool_CopiesTheJarWithoutABuildStage | VerifyPublish_WithAPrebuiltReleaseAndNoBuildTool_CopiesTheReleaseWithoutABuildStage | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_WithAPrebuiltJar_WithJarArtifactHasNoEffect | VerifyPublish_WithAPrebuiltRelease_WithMixReleaseArtifactHasNoEffect | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingAPrebuiltJarReincludesItAndItsDirectoriesInTheBuildContext | PublishingAPrebuiltReleaseReincludesItAndItsDirectoriesInTheBuildContext | NAK-498 | planned |
| AddJavaAppPublishTests.APrebuiltJarAlongsideAPomIsStillBuiltInTheImage | APrebuiltReleaseAlongsideAPomIsStillBuiltInTheImage | NAK-489 | planned |
| AddJavaAppPublishTests.VerifyPublish_PrebuiltJarImageBuildsAndRunsWithItsArguments | VerifyPublish_PrebuiltReleaseImageBuildsAndRunsWithItsArguments | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_WrapperBuiltImageBuildsTheProjectAndRunsIt | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_StagesTheFastJarDirectoryAndRunsQuarkusRunJar | VerifyPublish_Quarkus_StagesTheFastReleaseDirectoryAndRunsQuarkusRunRelease | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_Gradle_StagesFromTheGradleOutputDirectory | VerifyPublish_Quarkus_Gradle_StagesFromTheGradleOutputDirectory | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_FallsBackToASingleRunnerJarForUberJarPackaging | VerifyPublish_Quarkus_FallsBackToASingleRunnerReleaseForUberReleasePackaging | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_StagesTheDependencyDirectoryForLegacyJarPackaging | VerifyPublish_Quarkus_StagesTheDependencyDirectoryForLegacyReleasePackaging | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_StagesEveryPackagingTypeSoTheRunnerCanResolveItsDependencies | VerifyPublish_Quarkus_StagesEveryPackagingTypeSoTheRunnerCanResolveItsDependencies | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_Quarkus_WithJarArtifact_StagesThatFileInstead | VerifyPublish_Quarkus_WithMixReleaseArtifact_StagesThatFileInstead | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_InstallsUnzipWhenTheMavenWrapperPinsADistributionChecksum | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_DoesNotInstallUnzipWhenNoChecksumIsPinned | VerifyPublish_DoesNotInstallUnzipWhenNoChecksumIsPinned | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_RaisesTheBuildJdkWhenTheProjectTargetsAnOlderRelease | VerifyPublish_RaisesTheBuildJdkWhenTheProjectTargetsAnOlderRelease | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_RaisesTheBuildJdkForGradle9 | VerifyPublish_RaisesTheBuildJdkForGradle9 | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_KeepsTheBuildJdkOnTheTargetForAToolThatCannotRunOnANewerJdk | VerifyPublish_KeepsTheBuildJdkOnTheTargetForAToolThatCannotRunOnANewerJdk | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_RejectsAJarPathContainingWhitespace | VerifyPublish_RejectsAReleasePathContainingWhitespace | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_RejectsAnOtelAgentPathContainingWhitespace | VerifyPublish_RejectsAnOtlpExporterPathContainingWhitespace | NAK-495 | planned |
| AddJavaAppPublishTests.VerifyPublish_CapsTheBuildJdkAtWhatTheGradleWrapperCanRunOn | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_AcceptsTheFirstGradleReleaseThatRunsOnTheTargetedJdk | VerifyPublish_AcceptsTheFirstGradleReleaseThatRunsOnTheTargetedJdk | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_DoesNotApplyTheGradleCeilingToMaven | VerifyPublish_DoesNotApplyTheGradleCeilingToMaven | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_KeepsTheBuildJdkOnTheTargetWhenItIsNewEnough | VerifyPublish_KeepsTheBuildJdkOnTheTargetWhenItIsNewEnough | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_BuildsOnTheTargetPlatformWhenMavenSelectsNativeDependenciesByHostArchitecture | VerifyPublish_BuildsOnTheTargetPlatformWhenMavenSelectsNativeDependenciesByHostArchitecture | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_BuildsOnTheTargetPlatformWhenGradleUsesTheOsDetectorPlugin | VerifyPublish_BuildsOnTheTargetPlatformWhenGradleUsesTheOsDetectorPlugin | NAK-498 | planned |
| AddJavaAppPublishTests.PublishingWithAWrapperPathContainingWhitespaceIsRejected | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.PublishingAfterChangingTheWorkingDirectoryIsRejected | PublishingAfterChangingTheWorkingDirectoryIsRejected | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_ExplicitCompilerReleaseWinsOverTheSpringBootProperty | VerifyPublish_ExplicitCompilerReleaseWinsOverThePhoenixConfigProperty | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_CompilerPluginReleaseWinsOverTheSpringBootProperty | VerifyPublish_CompilerPluginReleaseWinsOverThePhoenixConfigProperty | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_SpringBootPropertyStillAppliesWhenItIsTheOnlySignal | VerifyPublish_PhoenixConfigPropertyStillAppliesWhenItIsTheOnlySignal | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_SpringBootPropertyResolvesThroughAnUnexpandedReference | VerifyPublish_PhoenixConfigPropertyResolvesThroughAnUnexpandedReference | NAK-499 | planned |
| AddJavaAppPublishTests.VerifyPublish_UsesTheNamedJarWhenTheBuildRunsInTheImage | VerifyPublish_UsesTheNamedReleaseWhenTheBuildRunsInTheImage | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_NamedJarWindowsSeparatorsBecomePosix | VerifyPublish_NamedReleaseWindowsSeparatorsBecomePosix | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_NamedJarOutsideTheContextIsRejected | VerifyPublish_NamedReleaseOutsideTheContextIsRejected | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_AWindowsAbsoluteJarPathIsRejectedOnEveryPlatform | n/a — tests Windows-specific path or batch-wrapper handling that the Elixir integration does not carry (mix paths are POSIX-normalized by BEAM tooling on every OS) | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_AWindowsAbsoluteJarArtifactIsRejectedOnEveryPlatform | n/a — tests Windows-specific path or batch-wrapper handling that the Elixir integration does not carry (mix paths are POSIX-normalized by BEAM tooling on every OS) | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_AWindowsAbsoluteWrapperIsRejectedOnEveryPlatform | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppPublishTests.VerifyPublish_ExplicitJarArtifactWinsOverTheNamedJar | VerifyPublish_ExplicitReleaseArtifactWinsOverTheNamedRelease | NAK-498 | planned |
| AddJavaAppPublishTests.VerifyPublish_MovingTheWorkingDirectoryAfterwardsFailsWithAnActionableMessage | VerifyPublish_MovingTheWorkingDirectoryAfterwardsFailsWithAnActionableMessage | NAK-498 | planned |
| AddJavaAppTests.AddJavaApp_MavenGoal_LaunchesThroughTheWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.AddJavaApp_GradleTask_LaunchesThroughTheWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.VerifyManifest_AddJavaAppWithJar | VerifyManifest_AddElixirAppWithMixRelease | NAK-494 | planned |
| AddJavaAppTests.VerifyManifest_AddJavaAppWithJarAndArgs | VerifyManifest_AddElixirAppWithMixReleaseAndArgs | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_SetsResourceName | AddElixirApp_SetsResourceName | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_UsesJavaAsCommand | AddElixirApp_UsesMixAsCommand | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_ResolvesWorkingDirectoryFullPath | AddElixirApp_ResolvesWorkingDirectoryFullPath | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_ImplementsIResourceWithServiceDiscovery | AddElixirApp_ImplementsIResourceWithServiceDiscovery | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_ImplementsIContainerFilesDestinationResource | AddElixirApp_ImplementsIContainerFilesDestinationResource | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithoutLaunchMode_ThrowsWhenArgumentsAreGathered | AddElixirApp_WithoutLaunchMode_ThrowsWhenArgumentsAreGathered | NAK-489 | planned |
| AddJavaAppTests.AddJavaAppWithJar_ArgsAreJarAndUserArgs | AddElixirAppWithMixRelease_ArgsAreReleaseAndUserArgs | NAK-494 | planned |
| AddJavaAppTests.AddJavaAppWithJar_NoUserArgs_OnlyJarArgs | AddElixirAppWithMixRelease_NoUserArgs_OnlyReleaseArgs | NAK-494 | planned |
| AddJavaAppTests.WithMavenGoalShouldThrowWhenBuilderIsNull | WithMixTaskShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoalShouldThrowWhenGoalIsNullOrEmpty | WithMixTaskShouldThrowWhenGoalIsNullOrEmpty | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoal_PassesGoalAsArgument | WithMixTask_PassesGoalAsArgument | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoal_WithArgs_IncludesGoalAndArgs | WithMixTask_WithArgs_IncludesGoalAndArgs | NAK-491 | planned |
| AddJavaAppTests.WithGradleTaskShouldThrowWhenBuilderIsNull | WithMixTaskShouldThrowWhenBuilderIsNull | NAK-491 | planned |
| AddJavaAppTests.WithGradleTaskShouldThrowWhenTaskIsNullOrEmpty | WithMixTaskShouldThrowWhenTaskIsNullOrEmpty | NAK-491 | planned |
| AddJavaAppTests.WithGradleTask_PassesTaskAsArgument | WithMixTask_PassesTaskAsArgument | NAK-491 | planned |
| AddJavaAppTests.WithGradleTask_WrapperPathIsResolved | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithMavenGoal_WrapperPathIsResolved | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithGradleTask_FindsTheWrapperAtTheRootOfAMultiProjectBuild | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithMavenGoal_FindsTheWrapperAtTheRootOfAMultiModuleBuild | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithGradleTask_PrefersTheWrapperInTheApplicationDirectory | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WrapperInvocationForWindowsRunsTheWrapperThroughCall | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WrapperInvocationForUnixRunsTheWrapperThroughShWithItsFullPath | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithGradleTask_WithArgs_IncludesTaskAndArgs | WithMixTask_WithArgs_IncludesTaskAndArgs | NAK-491 | planned |
| AddJavaAppTests.WithGradleTask_ThrowsWhenJarPathIsSet | WithMixTask_ThrowsWhenReleasePathIsSet | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoal_ThrowsWhenJarPathIsSet | WithMixTask_ThrowsWhenReleasePathIsSet | NAK-491 | planned |
| AddJavaAppTests.WithGradleTask_ThrowsWhenMavenGoalIsAlreadyConfigured | WithMixTask_ThrowsWhenMixTaskIsAlreadyConfigured | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoal_ThrowsWhenGradleTaskIsAlreadyConfigured | WithMixTask_ThrowsWhenMixTaskIsAlreadyConfigured | NAK-491 | planned |
| AddJavaAppTests.WithWrapperPathShouldThrowWhenBuilderIsNull | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPathShouldThrowWhenWrapperPathIsNullOrEmpty | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPath_OverridesMavenDefaultWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPath_OverridesGradleDefaultWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithJvmArgsShouldThrowWhenBuilderIsNull | WithMixEnvShouldThrowWhenBuilderIsNull | NAK-492 | planned |
| AddJavaAppTests.WithJvmArgsShouldThrowWhenArgsIsNull | WithMixEnvShouldThrowWhenArgsIsNull | NAK-492 | planned |
| AddJavaAppTests.WithJvmArgs_SetsJavaToolOptions | WithMixEnv_SetsMixEnvVars | NAK-492 | planned |
| AddJavaAppTests.WithJvmArgs_EmptyArgs_DoesNotSetJavaToolOptions | WithMixEnv_EmptyArgs_DoesNotSetMixEnvVars | NAK-492 | planned |
| AddJavaAppTests.WithJvmArgs_MultipleCalls_MergeValues | WithMixEnv_MultipleCalls_MergeValues | NAK-492 | planned |
| AddJavaAppTests.WithOtelAgentShouldThrowWhenBuilderIsNull | WithOtlpExporterShouldThrowWhenBuilderIsNull | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgentShouldThrowWhenAgentPathIsNullOrWhiteSpace | WithOtlpExporterShouldThrowWhenAgentPathIsNullOrWhiteSpace | NAK-495 | planned |
| AddJavaAppTests.AddJavaApp_ConfiguresOtlpExporterWithoutAnAgent | AddElixirApp_ConfiguresOtlpExporterWithoutAnAgent | NAK-489 | planned |
| AddJavaAppTests.WithOtelAgent_WithAgentPath_SetsJavaAgentInToolOptions | WithOtlpExporter_WithAgentPath_SetsJavaAgentInToolOptions | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgent_CalledTwice_UsesOnlyTheLastAgent | WithOtlpExporter_CalledTwice_UsesOnlyTheLastAgent | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgent_WithAgentPath_CombinedWithJvmArgs | WithOtlpExporter_WithAgentPath_CombinedWithMixEnv | NAK-495 | planned |
| AddJavaAppTests.WithMavenGoal_WithoutAWrapperOnDisk_IsRejectedWhenTheResourceStarts | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithGradleTask_WithoutAWrapperOnDisk_IsRejectedWhenTheResourceStarts | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPath_AfterTheBuildTool_WorksWhenTheProjectHasNoDefaultWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithMavenGoal_WrapperWithoutTheExecutableBit_StillProducesALaunchableCommandLine | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPath_PointingAtAMissingFile_IsRejectedWhenTheResourceStarts | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.WithWrapperPath_IsHonouredEvenWhenNoWrapperExistsOnDisk | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.AddJavaApp_RequestsSystemCertificateTrustScope | AddElixirApp_RequestsSystemCertificateTrustScope | NAK-495 | planned |
| AddJavaAppTests.AddJavaApp_WithAppendCertificateTrustScope_DoesNotOverrideTheJvmTrustStore | AddElixirApp_WithAppendCertificateTrustScope_DoesNotOverrideTheErlangCaStore | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgent_AgentPathContainingSpaces_IsQuotedForTheJvm | WithOtlpExporter_AgentPathContainingSpaces_IsQuotedForTheJvm | NAK-495 | planned |
| AddJavaAppTests.WithJvmArgs_ValueContainingSpaces_IsQuotedAfterTheAssignment | WithMixEnv_ValueContainingSpaces_IsQuotedAfterTheAssignment | NAK-492 | planned |
| AddJavaAppTests.WithOtelAgent_RelativeAgentPath_IsMadeAbsoluteInRunMode | WithOtlpExporter_RelativeAgentPath_IsMadeAbsoluteInRunMode | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgent_RelativeAgentPath_PointsAtContainerPathInPublishMode | WithOtlpExporter_RelativeAgentPath_PointsAtContainerPathInPublishMode | NAK-495 | planned |
| AddJavaAppTests.WithOtelAgent_AbsoluteAgentPath_IsLeftUnchangedInPublishMode | WithOtlpExporter_AbsoluteAgentPath_IsLeftUnchangedInPublishMode | NAK-495 | planned |
| AddJavaAppTests.WithMavenBuild_CreatesMavenBuildResourceInRunMode | WithMixDeps_CreatesMavenBuildResourceInRunMode | NAK-491 | planned |
| AddJavaAppTests.WithMavenBuild_CustomArgs_CreatesBuildResource | WithMixDeps_CustomArgs_CreatesBuildResource | NAK-491 | planned |
| AddJavaAppTests.WithGradleBuild_CreatesGradleBuildResourceInRunMode | WithMixDeps_CreatesGradleBuildResourceInRunMode | NAK-491 | planned |
| AddJavaAppTests.WithGradleBuild_CustomArgs_CreatesBuildResource | WithMixDeps_CustomArgs_CreatesBuildResource | NAK-491 | planned |
| AddJavaAppTests.WithMavenBuild_DoesNotCreateBuildResourceInPublishMode | WithMixDeps_DoesNotCreateBuildResourceInPublishMode | NAK-498 | planned |
| AddJavaAppTests.WithGradleBuild_DoesNotCreateBuildResourceInPublishMode | WithMixDeps_DoesNotCreateBuildResourceInPublishMode | NAK-498 | planned |
| AddJavaAppTests.WithBuildAndLaunch_DoesNotCreateASeparateBuildResource | WithBuildAndLaunch_DoesNotCreateASeparateBuildResource | NAK-489 | planned |
| AddJavaAppTests.WithMavenBuild_BuildResourceHasParentRelationship | WithMixDeps_BuildResourceHasParentRelationship | NAK-491 | planned |
| AddJavaAppTests.WithGradleBuild_BuildResourceHasParentRelationship | WithMixDeps_BuildResourceHasParentRelationship | NAK-491 | planned |
| AddJavaAppTests.AddJavaApp_WithJarPath_LaunchesTheJar | AddElixirApp_WithReleasePath_LaunchesTheRelease | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_InRunMode_SupportsDebugging | AddElixirApp_InRunMode_SupportsDebugging | NAK-497 | planned |
| AddJavaAppTests.AddJavaApp_InPublishMode_DoesNotAddDebuggingAnnotation | AddElixirApp_InPublishMode_DoesNotAddDebuggingAnnotation | NAK-497 | planned |
| AddJavaAppTests.WithJvmArgs_IdeDebugLaunchUsesJavaToolOptionsOnly | WithMixEnv_IdeDebugLaunchUsesMixEnvVarsOnly | NAK-497 | planned |
| AddJavaAppTests.WithMavenBuild_AndWithMavenGoal_ProduceTheSameGraphInEitherOrder | WithMixDeps_AndWithMixTask_ProduceTheSameGraphInEitherOrder | NAK-491 | planned |
| AddJavaAppTests.WithMavenGoal_ThenWithJvmArgs_SetsBothConfigurations | WithMixTask_ThenWithMixEnv_SetsBothConfigurations | NAK-491 | planned |
| AddJavaAppTests.WithGradleTask_ThenWithOtelAgent_SetsBothConfigurations | WithMixTask_ThenWithOtlpExporter_SetsBothConfigurations | NAK-495 | planned |
| AddJavaAppTests.WithWrapperPath_ThenWithMavenGoal_UsesCustomWrapper | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| AddJavaAppTests.VerifyManifest_WithMavenGoal | VerifyManifest_WithMixTask | NAK-491 | planned |
| AddJavaAppTests.VerifyManifest_WithGradleTask | VerifyManifest_WithMixTask | NAK-491 | planned |
| AddJavaAppTests.AddSpringBootApp_DebugConfigurationUsesTheDetectedBuildTool | AddPhoenixApp_DebugConfigurationUsesTheDetectedBuildTool | NAK-497 | planned |
| AddJavaAppTests.AddQuarkusApp_DebugConfigurationStartsTheFastJarTheBuildProduced | AddPhoenixApp_DebugConfigurationStartsTheFastReleaseTheBuildProduced | NAK-497 | planned |
| AddJavaAppTests.AddJavaApp_WithMavenBuild_SendsNoEntryPointWhenNothingOnDiskNamesOne | AddElixirApp_WithMixDeps_SendsNoEntryPointWhenNothingOnDiskNamesOne | NAK-491 | planned |
| AddJavaAppTests.AddJavaApp_WithJar_PutsTheArchiveOnTheClasspathAndLaunchesItsManifestMainClass | AddElixirApp_WithMixRelease_PutsTheArchiveOnTheClasspathAndLaunchesItsManifestMainClass | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_WithWindowsStyleJarPath_DebugsTheArchiveOnEveryPlatform | n/a — tests Windows-specific path or batch-wrapper handling that the Elixir integration does not carry (mix paths are POSIX-normalized by BEAM tooling on every OS) | NAK-497 | n/a |
| AddJavaAppTests.AddJavaApp_WithJar_ReadsAMainClassThatTheManifestWrappedAcrossLines | AddElixirApp_WithMixRelease_ReadsAMainClassThatTheManifestWrappedAcrossLines | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_WithJar_WithMainClass_PrefersTheExplicitMainClassOverTheManifest | AddElixirApp_WithMixRelease_WithMainClass_PrefersTheExplicitMainClassOverTheManifest | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_WithJar_ThatIsMissingOrHasNoMainClass_StillSendsTheClasspath | AddElixirApp_WithMixRelease_ThatIsMissingOrHasNoMainClass_StillSendsTheClasspath | NAK-494 | planned |
| AddJavaAppTests.AddJavaApp_WithoutAJar_LaunchesTheStartClassOfTheRepackagedSpringBootArchive | AddElixirApp_WithoutAJar_LaunchesTheStartClassOfTheRepackagedSpringBootArchive | NAK-493 | planned |
| AddJavaAppTests.AddJavaApp_WithoutAJar_IgnoresTheArchivesABuildLeavesAlongsideTheApplication | AddElixirApp_WithoutAJar_IgnoresTheArchivesABuildLeavesAlongsideTheApplication | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithoutAJar_SendsNoMainClassWhenTheBuildOutputIsAmbiguous | AddElixirApp_WithoutAJar_SendsNoMainClassWhenTheBuildOutputIsAmbiguous | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithoutAJar_SendsNoMainClassWhenTheArchiveOnlyNamesASpringBootLauncher | AddElixirApp_WithoutAJar_SendsNoMainClassWhenTheArchiveOnlyNamesASpringBootLauncher | NAK-493 | planned |
| AddJavaAppTests.AddJavaApp_WithoutAJar_PrefersAnExplicitMainClassOverTheBuildOutput | AddElixirApp_WithoutAJar_PrefersAnExplicitMainClassOverTheBuildOutput | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithNoResolvableEntryPoint_NamesTheMavenProjectSoTheIdeDoesNotPrompt | AddElixirApp_WithNoResolvableEntryPoint_NamesTheMixProjectSoTheIdeDoesNotPrompt | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithNoResolvableEntryPoint_NamesTheGradleProjectFromItsSettingsFile | AddElixirApp_WithNoResolvableEntryPoint_NamesTheMixProjectFromItsSettingsFile | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithNoResolvableEntryPoint_FallsBackToTheDirectoryNameGradleWouldUse | AddElixirApp_WithNoResolvableEntryPoint_FallsBackToTheDirectoryNameGradleWouldUse | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WhenTheProjectDirectoryIsNamedDifferently_DoesNotGuessTheIdeProjectName | AddElixirApp_WhenTheProjectDirectoryIsNamedDifferently_DoesNotGuessTheAppName | NAK-489 | planned |
| AddJavaAppTests.AddJavaApp_WithAnExplicitJar_DoesNotScopeResolutionToAProject | AddElixirApp_WithAnExplicitRelease_DoesNotScopeResolutionToAProject | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainerShouldThrowWhenBuilderIsNull | AddElixirContainerShouldThrowWhenBuilderIsNull | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainerShouldThrowWhenNameIsNullOrWhitespace | AddElixirContainerShouldThrowWhenNameIsNullOrWhitespace | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainerShouldThrowWhenImageIsNullOrWhitespace | AddElixirContainerShouldThrowWhenImageIsNullOrWhitespace | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainer_UsesTheRequestedImageAndTag | AddElixirContainer_UsesTheRequestedImageAndTag | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainer_WithoutTag_LeavesTheTagToTheContainerRuntime | AddElixirContainer_WithoutTag_LeavesTheTagToTheContainerRuntime | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainer_IsDiscoverableAndUsesTheJavaIcon | AddElixirContainer_IsDiscoverableAndUsesTheJavaIcon | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainer_DeclaresNoEndpoint | AddElixirContainer_DeclaresNoEndpoint | NAK-489 | planned |
| AddJavaContainerTests.AddJavaContainer_ExportsTelemetryToAspire | AddElixirContainer_ExportsTelemetryToAspire | NAK-489 | planned |
| AddJavaContainerTests.WithJvmArgs_AppliesToAContainerToo | WithMixEnv_AppliesToAContainerToo | NAK-492 | planned |
| AddJavaContainerTests.AddJavaContainer_KeepsTheJvmBuiltInCertificateAuthorities | AddElixirContainer_KeepsTheJvmBuiltInCertificateAuthorities | NAK-489 | planned |
| AddQuarkusAppTests.AddQuarkusApp_InADebugSession_AddsABuildTheApplicationWaitsFor | AddPhoenixApp_InADebugSession_AddsABuildTheApplicationWaitsFor | NAK-497 | planned |
| AddQuarkusAppTests.AddQuarkusApp_OutsideADebugSession_AddsNoBuild | AddPhoenixApp_OutsideADebugSession_AddsNoBuild | NAK-497 | planned |
| AddQuarkusAppTests.AddSpringBootApp_InADebugSession_AddsNoBuild | AddPhoenixApp_InADebugSession_AddsNoBuild | NAK-497 | planned |
| AddQuarkusAppTests.AddQuarkusApp_MavenProject_LaunchesInDevMode | AddPhoenixApp_MixProject_LaunchesInDevMode | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_GradleProject_LaunchesInDevMode | AddPhoenixApp_MixProject_LaunchesInDevMode | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_DoesNotCreateASeparateBuildResource | AddPhoenixApp_DoesNotCreateASeparateBuildResource | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_DeclaresHttpEndpointThroughQuarkusHttpPort | AddPhoenixApp_DeclaresHttpEndpointThroughPhoenixEndpointPort | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_GradleProject_DeclaresHttpEndpointThroughQuarkusHttpPort | AddPhoenixApp_MixProject_DeclaresHttpEndpointThroughPhoenixEndpointPort | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_SetsTheDevProfileInRunMode | AddPhoenixApp_SetsTheMixEnvDevInRunMode | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_DoesNotSetTheDevProfileWhenPublishing | AddPhoenixApp_DoesNotSetTheMixEnvDevWhenPublishing | NAK-499 | planned |
| AddQuarkusAppTests.AddQuarkusApp_DisablesTheObservabilityDevServiceInRunMode | AddPhoenixApp_DisablesThePhoenixDevObservabilityInRunMode | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_MirrorsTheOtlpConfigurationOntoTheNamesQuarkusReads | AddPhoenixApp_MirrorsTheOtlpConfigurationOntoTheNamesQuarkusReads | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_DoesNotMirrorTheOtlpConfigurationWhenPublishing | AddPhoenixApp_DoesNotMirrorTheOtlpConfigurationWhenPublishing | NAK-499 | planned |
| AddQuarkusAppTests.AddQuarkusApp_AddsNoHealthCheck | AddPhoenixApp_AddsNoHealthCheck | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_NoBuildFile_ThrowsWhenTheResourceStarts | AddPhoenixApp_NoBuildFile_ThrowsWhenTheResourceStarts | NAK-493 | planned |
| AddQuarkusAppTests.AddQuarkusApp_RemainsAJavaAppResource | AddPhoenixApp_RemainsAElixirAppResource | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_MavenProject_LaunchesThroughSpringBootRun | AddPhoenixApp_MixProject_LaunchesThroughSpringPhoenixServer | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_GradleProject_LaunchesThroughBootRun | AddPhoenixApp_MixProject_LaunchesThroughPhoenixServer | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_KotlinGradleProject_IsDetectedAsGradle | AddPhoenixApp_KotlinMixProject_IsDetectedAsGradle | NAK-493 | planned |
| AddSpringBootAppTests.BuildToolDetection_GradleMarkersAgreeBetweenRunAndPublish | ToolVersionsDetection_GradleMarkersAgreeBetweenRunAndPublish | NAK-498 | planned |
| AddSpringBootAppTests.AddSpringBootApp_DoesNotCreateASeparateBuildResource | AddPhoenixApp_DoesNotCreateASeparateBuildResource | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_ExplicitLaunchGoalOverridesTheDetectedDefault | AddPhoenixApp_ExplicitLaunchGoalOverridesTheDetectedDefault | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_ExplicitBuildArgumentsDoNotCreateASeparateBuildResource | AddPhoenixApp_ExplicitBuildArgumentsDoNotCreateASeparateBuildResource | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_DeclaresHttpEndpointThroughServerPort | AddPhoenixApp_DeclaresHttpEndpointThroughPhoenixEndpointPort | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_AddsNoHealthCheck | AddPhoenixApp_AddsNoHealthCheck | NAK-493 | planned |
| AddSpringBootAppTests.AddSpringBootApp_NoBuildFile_ThrowsWhenTheResourceStarts | AddPhoenixApp_NoBuildFile_ThrowsWhenTheResourceStarts | NAK-493 | planned |
| AddSpringBootAppTests.BuildToolDetection_BothBuildToolsAreRejectedInRunAndPublish | ToolVersionsDetection_BothBuildToolsAreRejectedInRunAndPublish | NAK-498 | planned |
| AddSpringBootAppTests.WithOtelAgent_NoPath_ResolvesTheBuildToolsOutputDirectory | WithOtlpExporter_NoPath_ResolvesTheBuildToolsOutputDirectory | NAK-495 | planned |
| AddSpringBootAppTests.WithOtelAgent_NoPath_ResolvesTheBuildToolConfiguredAfterIt | WithOtlpExporter_NoPath_ResolvesTheBuildToolConfiguredAfterIt | NAK-495 | planned |
| AddSpringBootAppTests.WithOtelAgent_BuildProducedAgent_AddsABuildTheApplicationWaitsFor | WithOtlpExporter_BuildProducedAgent_AddsABuildTheApplicationWaitsFor | NAK-495 | planned |
| AddSpringBootAppTests.WithOtelAgent_AbsoluteAgentPath_AddsNoBuild | WithOtlpExporter_AbsoluteAgentPath_AddsNoBuild | NAK-495 | planned |
| AddSpringBootAppTests.WithOtelAgent_NoPath_WithoutABuild_Throws | WithOtlpExporter_NoPath_WithoutABuild_Throws | NAK-495 | planned |
| AddSpringBootAppTests.AddSpringBootApp_RemainsAJavaAppResource | AddPhoenixApp_RemainsAElixirAppResource | NAK-493 | planned |
| AddSpringBootAppTests.WithMavenBuildThenWithOtelAgent_CreatesTheBuildResourceTheAgentNeeds | WithMixDepsThenWithOtlpExporter_CreatesTheBuildResourceTheAgentNeeds | NAK-495 | planned |
| JavaBuildToolResolverTests.ResolveWrapperPath_UsesTheRequestedPlatformsDefault | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaBuildToolResolverTests.ResolveWrapperPath_UsesWithWrapperPathOnEveryPlatform | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaBuildToolResolverTests.ResolveWrapperPath_UsesAnAncestorWrapperAtTheBuildRoot | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaBuildToolResolverTests.ResolveWrapperPath_IgnoresAnAncestorWrapperInAWorldWritableDirectory | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaBuildToolResolverTests.ResolveWrapperPath_UsesAnAncestorWrapperInAGroupWritableDirectory | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaBuildToolResolverTests.ResolveWrapperPath_IgnoresAnAncestorWrapperThatIsItselfWorldWritable | n/a — mix has no per-project wrapper script (gradlew/mvnw); the mix executable is installed once through the version manager (mise/asdf), so wrapper discovery and trust checks do not apply | NAK-498 | n/a |
| JavaPublicApiTests.CtorJavaAppResourceShouldThrowWhenNameIsNullOrEmpty | CtorElixirAppResourceShouldThrowWhenNameIsNullOrEmpty | NAK-489 | written |

## Rust CodeGen

M2 source: `tests/Aspire.Hosting.CodeGeneration.Rust.Tests`. Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AtsRustCodeGeneratorTests.Language_ReturnsRust | Language_ReturnsElixir | NAK-508 | planned |
| AtsRustCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_CapturesParameters | GenerateDistributedApplication_WithTestTypes_CapturesParameters | NAK-509 | planned |
| AtsRustCodeGeneratorTests.Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | NAK-509 | planned |
| AtsRustCodeGeneratorTests.Scanner_AddTestRedis_HasCorrectTypeMetadata | Scanner_AddTestRedis_HasCorrectTypeMetadata | NAK-509 | planned |
| AtsRustCodeGeneratorTests.Scanner_WithPersistence_HasCorrectExpandedTargets | Scanner_WithPersistence_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsRustCodeGeneratorTests.Scanner_WithOptionalString_HasCorrectExpandedTargets | Scanner_WithOptionalString_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsRustCodeGeneratorTests.Scanner_HostingAssembly_AddContainerCapability | Scanner_HostingAssembly_AddContainerCapability | NAK-509 | planned |
| AtsRustCodeGeneratorTests.RuntimeType_ContainerResource_IsNotInterface | RuntimeType_ContainerResource_IsNotInterface | NAK-508 | planned |
| AtsRustCodeGeneratorTests.TwoPassScanning_DeduplicatesCapabilities | TwoPassScanning_DeduplicatesCapabilities | NAK-509 | planned |
| AtsRustCodeGeneratorTests.TwoPassScanning_MergesHandleTypesFromAllAssemblies | TwoPassScanning_MergesHandleTypesFromAllAssemblies | NAK-509 | planned |
| AtsRustCodeGeneratorTests.TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GeneratedCode_UsesSnakeCaseMethodNames | GeneratedCode_UsesSnakeCaseFunctionNames | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GeneratedCode_HasCreateBuilderFunction | GeneratedCode_HasCreateBuilderFunction | NAK-509 | planned |
| AtsRustCodeGeneratorTests.GeneratedCode_HasModRsFile | GeneratedCode_HasContextExFile | NAK-509 | planned |
| RustLanguageSupportTests.Scaffold_CreatesRustAppHostFilesOnly | Scaffold_CreatesElixirAppHostFilesOnly | NAK-508 | planned |
| RustLanguageSupportTests.Detect_ReturnsRustAppHostWhenMarkerAndCargoExist | Detect_ReturnsElixirAppHostWhenMarkerAndCargoExist | NAK-508 | planned |
| RustLanguageSupportTests.Detect_DoesNotTreatTypeScriptAppHostAsRust | Detect_DoesNotTreatTypeScriptAppHostAsElixir | NAK-508 | planned |
| RustLanguageSupportTests.Detect_RequiresCargoManifest | Detect_RequiresMixExs | NAK-508 | planned |
| RustLanguageSupportTests.GetRuntimeSpec_UsesCargoRun | GetRuntimeSpec_UsesMixRun | NAK-508 | planned |
| RustLanguageSupportTests.GetRuntimeSpec_NamesTheScaffoldedBinarySoASecondBinTargetStaysUnambiguous | GetRuntimeSpec_NamesTheScaffoldedBinarySoASecondBinTargetStaysUnambiguous | NAK-508 | planned |

## Go CodeGen

M2 source: `tests/Aspire.Hosting.CodeGeneration.Go.Tests`. Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AtsGoCodeGeneratorTests.Language_ReturnsGo | Language_ReturnsElixir | NAK-508 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_CapturesParameters | GenerateDistributedApplication_WithTestTypes_CapturesParameters | NAK-509 | planned |
| AtsGoCodeGeneratorTests.Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | NAK-509 | planned |
| AtsGoCodeGeneratorTests.Scanner_AddTestRedis_HasCorrectTypeMetadata | Scanner_AddTestRedis_HasCorrectTypeMetadata | NAK-509 | planned |
| AtsGoCodeGeneratorTests.Scanner_WithPersistence_HasCorrectExpandedTargets | Scanner_WithPersistence_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsGoCodeGeneratorTests.Scanner_WithOptionalString_HasCorrectExpandedTargets | Scanner_WithOptionalString_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsGoCodeGeneratorTests.Scanner_HostingAssembly_AddContainerCapability | Scanner_HostingAssembly_AddContainerCapability | NAK-509 | planned |
| AtsGoCodeGeneratorTests.RuntimeType_ContainerResource_IsNotInterface | RuntimeType_ContainerResource_IsNotInterface | NAK-508 | planned |
| AtsGoCodeGeneratorTests.TwoPassScanning_DeduplicatesCapabilities | TwoPassScanning_DeduplicatesCapabilities | NAK-509 | planned |
| AtsGoCodeGeneratorTests.TwoPassScanning_MergesHandleTypesFromAllAssemblies | TwoPassScanning_MergesHandleTypesFromAllAssemblies | NAK-509 | planned |
| AtsGoCodeGeneratorTests.TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_UsesPascalCaseMethodNames | GeneratedCode_UsesSnakeCaseFunctionNames | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_HasCreateBuilderFunction | GeneratedCode_HasCreateBuilderFunction | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_CreateBuilderDefaultsAppHostFilePathFromEnvironment | GeneratedCode_CreateBuilderDefaultsAppHostFilePathFromEnvironment | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_CreateBuilderOmitsEmptyDashboardApplicationName | GeneratedCode_CreateBuilderOmitsEmptyDashboardApplicationName | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_DtoCallbacksReturnMutatedArguments | GeneratedCode_DtoCallbacksReturnMutatedArguments | NAK-510 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_CallbackArgsSkipUndecodableStructFields | GeneratedCode_CallbackArgsSkipUndecodableStructFields | NAK-511 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_HasGoModFile | GeneratedCode_HasMixExsFile | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GenerateDistributedApplication_HostingAssembly_SanitizesGoKeywordParameters | GenerateDistributedApplication_HostingAssembly_SanitizesElixirKeywordParameters | NAK-509 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_FlattensSingleOptionalDtoOptionsParameter | GeneratedCode_FlattensSingleOptionalDtoOptionsParameter | NAK-510 | planned |
| AtsGoCodeGeneratorTests.GeneratedCode_DoesNotFlattenWhenOptionsCoexistsWithCancellationToken | GeneratedCode_DoesNotFlattenWhenOptionsCoexistsWithCancellationToken | NAK-510 | planned |

## Python CodeGen

M2 source: `tests/Aspire.Hosting.CodeGeneration.Python.Tests`. Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AtsPythonCodeGeneratorTests.Language_ReturnsPython | Language_ReturnsElixir | NAK-508 | planned |
| AtsPythonCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_CapturesParameters | GenerateDistributedApplication_WithTestTypes_CapturesParameters | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.Scanner_AddTestRedis_HasCorrectTypeMetadata | Scanner_AddTestRedis_HasCorrectTypeMetadata | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.Scanner_WithPersistence_HasCorrectExpandedTargets | Scanner_WithPersistence_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.Scanner_WithOptionalString_HasCorrectExpandedTargets | Scanner_WithOptionalString_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.Scanner_HostingAssembly_AddContainerCapability | Scanner_HostingAssembly_AddContainerCapability | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.RuntimeType_ContainerResource_IsNotInterface | RuntimeType_ContainerResource_IsNotInterface | NAK-508 | planned |
| AtsPythonCodeGeneratorTests.TwoPassScanning_DeduplicatesCapabilities | TwoPassScanning_DeduplicatesCapabilities | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.TwoPassScanning_MergesHandleTypesFromAllAssemblies | TwoPassScanning_MergesHandleTypesFromAllAssemblies | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_UsesSnakeCaseMethodNames | GeneratedCode_UsesSnakeCaseFunctionNames | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_HasCreateBuilderFunction | GeneratedCode_HasCreateBuilderFunction | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_CreateBuilderDefaultsAppHostFilePathFromEnvironment | GeneratedCode_CreateBuilderDefaultsAppHostFilePathFromEnvironment | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_UsesTypeHints | GeneratedCode_UsesTypespecs | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_SanitizesPythonKeywordIdentifiers | GeneratedCode_SanitizesElixirKeywordIdentifiers | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_PreservesAcronymsInSnakeCaseIdentifiers | GeneratedCode_PreservesAcronymsInSnakeCaseIdentifiers | NAK-509 | planned |
| AtsPythonCodeGeneratorTests.GeneratedCode_SanitizesClrGenericNamesInInheritance | GeneratedCode_SanitizesClrGenericNamesInBehaviour | NAK-509 | planned |

## Java CodeGen

M2 source: `tests/Aspire.Hosting.CodeGeneration.Java.Tests`. Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AtsJavaCodeGeneratorTests.Language_ReturnsJava | Language_ReturnsElixir | NAK-508 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_DeclaresNumericParametersAsNumber | GenerateDistributedApplication_DeclaresNumericParametersAsNumber | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_CapturesParameters | GenerateDistributedApplication_WithTestTypes_CapturesParameters | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_AddTestRedis_HasCorrectTypeMetadata | Scanner_AddTestRedis_HasCorrectTypeMetadata | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_WithPersistence_HasCorrectExpandedTargets | Scanner_WithPersistence_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_WithOptionalString_HasCorrectExpandedTargets | Scanner_WithOptionalString_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_HostingAssembly_AddContainerCapability | Scanner_HostingAssembly_AddContainerCapability | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.Scanner_HostingAssembly_FluentBuilderCapabilities_ReturnBuilder | Scanner_HostingAssembly_FluentBuilderCapabilities_ReturnBuilder | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_HostingAssembly_FluentBuilderMethods_ReturnConcreteBuilderType | GeneratedCode_HostingAssembly_FluentBuilderMethods_ReturnConcreteBuilderType | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.RuntimeType_ContainerResource_IsNotInterface | RuntimeType_ContainerResource_IsNotInterface | NAK-508 | planned |
| AtsJavaCodeGeneratorTests.TwoPassScanning_DeduplicatesCapabilities | TwoPassScanning_DeduplicatesCapabilities | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.TwoPassScanning_MergesHandleTypesFromAllAssemblies | TwoPassScanning_MergesHandleTypesFromAllAssemblies | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.TwoPassScanning_GeneratesDerivedResourceInheritance | TwoPassScanning_GeneratesDerivedResourceInheritance | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_UsesCamelCaseMethodNames | GeneratedCode_UsesSnakeCaseFunctionNames | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_HasCreateBuilderMethod | GeneratedCode_HasCreateBuilderMethod | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_HasPublicAspireClass | GeneratedCode_HasPublicAspireModule | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedTransport_HandlesJsonRpcArrayCallbackParameters | GeneratedTransport_HandlesJsonRpcArrayCallbackParameters | NAK-511 | planned |
| AtsJavaCodeGeneratorTests.GeneratedDtoValues_AreSerializedAsMaps | GeneratedDtoValues_AreSerializedAsMaps | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_SuppressesWarningsOnEveryGeneratedType | GeneratedCode_SuppressesWarningsOnEveryGeneratedType | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_DoesNotEmitWildcardImports | GeneratedCode_DoesNotEmitUnusedAliases | NAK-509 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_OnlyImportsTypesTheFileReferences | GeneratedCode_OnlyImportsTypesTheFileReferences | NAK-511 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCode_RegistersCollectionWrappersWithoutRawTypes | GeneratedCode_RegistersCollectionWrappersWithoutRawTypes | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.GeneratedCollectionWrappers_ExposeTheSameOperationsAsTheOtherLanguages | GeneratedCollectionWrappers_ExposeTheSameOperationsAsTheOtherLanguages | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.GenerateDistributedApplication_EscapesDtoPropertiesNamedAfterJavaKeywords | GenerateDistributedApplication_EscapesDtoPropertiesNamedAfterElixirKeywords | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.DtoPropertyWithDictionaryNestedInArrayCastsToTheFieldType | DtoPropertyWithDictionaryNestedInArrayCastsToTheFieldType | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.DtoPropertyWithDictionaryNestedInListCastsToTheFieldType | DtoPropertyWithDictionaryNestedInListCastsToTheFieldType | NAK-510 | planned |
| AtsJavaCodeGeneratorTests.ExportedDtoValueInitializerCallsTheEscapedSetter | ExportedDtoValueInitializerCallsTheEscapedSetter | NAK-510 | planned |
| JavaLanguageSupportDetectionTests.DetectFindsTheAppHostInEverySupportedLayout | DetectFindsTheAppHostInEverySupportedLayout | NAK-508 | planned |
| JavaLanguageSupportDetectionTests.DetectPrefersTheFlatLayoutWhenBothArePresent | DetectPrefersTheFlatLayoutWhenBothArePresent | NAK-508 | planned |
| JavaLanguageSupportDetectionTests.DetectReportsNotFoundForADirectoryWithNoAppHost | DetectReportsNotFoundForADirectoryWithNoAppHost | NAK-508 | planned |
| JavaLanguageSupportDetectionTests.DetectIgnoresOtherJavaSources | DetectIgnoresOtherElixirSources | NAK-508 | planned |
| JavaLanguageSupportTests.GetRuntimeSpec_SetsCompileUpToDateCheckWhenSupported | GetRuntimeSpec_SetsCompileUpToDateCheckWhenSupported | NAK-508 | planned |
| JavaLanguageSupportTests.SetCompileUpToDateCheckIfSupported_PopulatesCompileUpToDateCheckOnCommandSpec | SetCompileUpToDateCheckIfSupported_PopulatesCompileUpToDateCheckOnCommandSpec | NAK-509 | planned |
| JavaLanguageSupportTests.SetCompileUpToDateCheckIfSupported_IgnoresLegacyCommandSpec | SetCompileUpToDateCheckIfSupported_IgnoresLegacyCommandSpec | NAK-509 | planned |

## TypeScript CodeGen

M2 source: `tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests`. Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests`.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| AtsTypeScriptCodeGeneratorTests.Language_ReturnsTypeScript | Language_ReturnsElixir | NAK-508 | planned |
| AtsTypeScriptCodeGeneratorTests.EmbeddedResource_PackageJson_IsAvailableWithExpectedStructure | EmbeddedResource_PackageJson_IsAvailableWithExpectedStructure | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_EmitsBaseAndTransportResourcesVerbatim | GenerateDistributedApplication_EmitsBaseAndTransportResourcesVerbatim | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | GenerateDistributedApplication_WithTestTypes_IncludesExportedValues | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithHostingTypes_KeepsReferenceExpressionInBaseTs | GenerateDistributedApplication_WithHostingTypes_KeepsReferenceExpressionInBaseTs | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | GenerateDistributedApplication_WithTestTypes_IncludesCapabilities | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_CapturesParameters | GenerateDistributedApplication_WithTestTypes_CapturesParameters | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithTestTypes_CapturesXmlDocumentation | Scanner_WithTestTypes_CapturesXmlDocumentation | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithTestTypes_EmitsXmlDocumentationAsJSDoc | GenerateDistributedApplication_WithTestTypes_EmitsXmlDocumentationAsModuleDoc | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithSuppressedSummary_DoesNotUseDescriptionFallback | GenerateDistributedApplication_WithSuppressedSummary_DoesNotUseDescriptionFallback | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithVoidReturn_DoesNotEmitReturnsDocumentation | GenerateDistributedApplication_WithVoidReturn_DoesNotEmitReturnsDocumentation | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithAtsReference_RendersJsDocLink | GenerateDistributedApplication_WithAtsReference_RendersModuleDocLink | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithContextType_GeneratesPropertyCapabilities | GenerateDistributedApplication_WithContextType_GeneratesPropertyCapabilities | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_TestRedisResource_ImplementsIResource | Scanner_TestRedisResource_ImplementsIResource | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithOptionalString_TargetsIResource | Scanner_WithOptionalString_TargetsIResource | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithOptionalString_ExpandsToTestRedis | Scanner_WithOptionalString_ExpandsToTestRedis | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_BaseTypeChain_CollectsInterfacesAcrossAssemblies | Scanner_BaseTypeChain_CollectsInterfacesAcrossAssemblies | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_AddTestRedis_HasCorrectTypeMetadata | Scanner_AddTestRedis_HasCorrectTypeMetadata | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | Scanner_ReturnsBuilder_TrueForResourceBuilderReturnTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.FactoryMethod_ReturnsChildResourceType_NotParentType | FactoryMethod_ReturnsChildResourceType_NotParentType | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithPersistence_HasCorrectExpandedTargets | Scanner_WithPersistence_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithOptionalString_HasCorrectExpandedTargets | Scanner_WithOptionalString_HasCorrectExpandedTargets | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_HostingAssembly_AddContainerCapability | Scanner_HostingAssembly_AddContainerCapability | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_BrowsersAssembly_WithBrowserLogsCapability | Scanner_BrowsersAssembly_WithBrowserLogsCapability | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_HostingAssembly_ContainerResourceCapabilities | Scanner_HostingAssembly_ContainerResourceCapabilities | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.RuntimeType_ContainerResource_IsNotInterface | RuntimeType_ContainerResource_IsNotInterface | NAK-508 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ContainerResource_DirectTargetingHasCorrectIsInterface | Scanner_ContainerResource_DirectTargetingHasCorrectIsInterface | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_GenericConstraintWithClassType_CorrectlyIdentifiesAsNotInterface | Scanner_GenericConstraintWithClassType_CorrectlyIdentifiesAsNotInterface | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Pattern2_InterfaceTypeDirectly_IsDiscoveredAndExpanded | Pattern2_InterfaceTypeDirectly_IsDiscoveredAndExpanded | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Pattern3_ConcreteTypeWithInheritance_ExpandsToDerivedTypes | Pattern3_ConcreteTypeWithInheritance_ExpandsToDerivedTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Pattern3_ConcreteTypeFromHosting_ExpandsToDerivedTypes | Pattern3_ConcreteTypeFromHosting_ExpandsToDerivedTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Pattern4_InterfaceParameterType_HasCorrectTypeRef | Pattern4_InterfaceParameterType_HasCorrectTypeRef | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Pattern4_InterfaceParameterType_GeneratesUnionType | Pattern4_InterfaceParameterType_GeneratesUnionType | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.AspireUnion_InterfaceHandleInput_GeneratesExpandedUnion | AspireUnion_InterfaceHandleInput_GeneratesExpandedUnion | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.MapInputUnionTypeToTypeScript_ThrowsOnEmptyUnion | MapInputUnionTypeToElixir_ThrowsOnEmptyUnion | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_BaseTypeHierarchy_IsCollected | Scanner_BaseTypeHierarchy_IsCollected | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.BugFix_SyntheticTypeInfo_CorrectlyIdentifiesInterfaceTypes | BugFix_SyntheticTypeInfo_CorrectlyIdentifiesInterfaceTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.BugFix_InterfaceExpansion_WorksAcrossAssemblies | BugFix_InterfaceExpansion_WorksAcrossAssemblies | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.BugFix_TargetParameterName_IsPopulatedFromMethodSignature | BugFix_TargetParameterName_IsPopulatedFromMethodSignature | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_HostingAssembly_UsesUnifiedWithReferenceCapability | Scanner_HostingAssembly_UsesUnifiedWithReferenceCapability | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.BugFix_TargetParameterName_WithVolumeUsesResource | BugFix_TargetParameterName_WithVolumeUsesResource | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.TwoPassScanning_DeduplicatesCapabilities | TwoPassScanning_DeduplicatesCapabilities | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.TwoPassScanning_MergesHandleTypesFromAllAssemblies | TwoPassScanning_MergesHandleTypesFromAllAssemblies | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_EmitsPromiseWrapperForBareMarkerResourceBuilder | GenerateDistributedApplication_EmitsOkTupleWrapperForBareMarkerResourceBuilder | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_DoesNotEmitUnusedPromiseWrappersForParameterOnlyResources | GenerateDistributedApplication_DoesNotEmitUnusedOkTupleWrappersForParameterOnlyResources | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_DoesNotEmitUnusedPromiseWrappersForMutablePropertyResources | GenerateDistributedApplication_DoesNotEmitUnusedOkTupleWrappersForMutablePropertyResources | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_EmitsPromiseWrapperForReturnedInterfaceAlias | GenerateDistributedApplication_EmitsOkTupleWrapperForReturnedInterfaceAlias | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_RejectsUnrelatedResourceTypesWithSameGeneratedName | GenerateDistributedApplication_RejectsUnrelatedResourceTypesWithSameGeneratedName | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_EveryReferencedPromiseWrapperIsDeclared | GenerateDistributedApplication_EveryReferencedOkTupleWrapperIsDeclared | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_EveryRpcHandleMatchesTheConstructedWrapper | GenerateDistributedApplication_EveryRpcHandleMatchesTheConstructedWrapper | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | TwoPassScanning_GeneratesWithEnvironmentOnTestRedisBuilder | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.TwoPassScanning_DeduplicatesExpandedUnionTypes | TwoPassScanning_DeduplicatesExpandedUnionTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithDtoCallbackOptions_MarshalsNestedCallbackProperties | GenerateDistributedApplication_WithDtoCallbackOptions_MarshalsNestedCallbackProperties | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_AzureProvisioningCallbacks_ExposeTypedCustomizationProperties | Scanner_AzureProvisioningCallbacks_ExposeTypedCustomizationProperties | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_AzureExistingResourceScopes_ExposeTypeScriptCapabilities | Scanner_AzureExistingResourceScopes_ExposeElixirCapabilities | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateDistributedApplication_WithAzureExistingResourceScopes_EmitsTypeScriptMethods | GenerateDistributedApplication_WithAzureExistingResourceScopes_EmitsElixirMethods | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_HostingAssembly_CollectionIntrinsicsAreRegistered | Scanner_HostingAssembly_CollectionIntrinsicsAreRegistered | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_HostingAssembly_IncludesCoreFrameworkPolyglotHelpers | Generate_HostingAssembly_IncludesCoreFrameworkPolyglotHelpers | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ObjectParameter_MapsToAny | Scanner_ObjectParameter_MapsToAny | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.AspireUnionAttribute_ParsesCorrectly | AspireUnionAttribute_ParsesCorrectly | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_InstanceMethod_HasCorrectCapabilityKind | Scanner_InstanceMethod_HasCorrectCapabilityKind | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ReferenceExpressionGetValueAsync_IsExported | Scanner_ReferenceExpressionGetValueAsync_IsExported | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ExtensionMethod_HasCorrectCapabilityKind | Scanner_ExtensionMethod_HasCorrectCapabilityKind | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_TypeWithMethods_CreatesThenableWrapper | Generate_TypeWithMethods_CreatesOkTupleWrapper | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_TypeWithOnlyProperties_NoThenableWrapper | Generate_TypeWithOnlyProperties_NoOkTupleWrapper | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_VoidInstanceMethod_ReturnsContainingTypePromise | Generate_VoidInstanceMethod_ReturnsContainingTypePromise | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_PrimitiveReturningMethod_ReturnsPlainPromise | Generate_PrimitiveReturningMethod_ReturnsOkTuple | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.GenerateTwoPassCode_UsesUnifiedWithReferenceSurface | GenerateTwoPassCode_UsesUnifiedWithReferenceSurface | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_CancellationToken_MapsToCorrectTypeId | Scanner_CancellationToken_MapsToCorrectTypeId | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_MethodWithCancellationToken_GeneratesCancellationTokenParameter | Generate_MethodWithCancellationToken_GeneratesCancellationTokenParameter | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_CancellationTokenInCallback_MapsCorrectly | Scanner_CancellationTokenInCallback_MapsCorrectly | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_CancellationTokenWithOtherParams_AllParamsPresent | Scanner_CancellationTokenWithOtherParams_AllParamsPresent | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_AspireDtoType_IsDiscovered | Scanner_AspireDtoType_IsDiscovered | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_AspireDtoType_GeneratesInterface | Generate_AspireDtoType_GeneratesInterface | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_NestedDtoType_GeneratesCorrectTypes | Generate_NestedDtoType_GeneratesCorrectTypes | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_DeeplyNestedDto_IsDiscovered | Scanner_DeeplyNestedDto_IsDiscovered | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_EnumType_IsDiscovered | Scanner_EnumType_IsDiscovered | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_EnumType_GeneratesStringEnum | Generate_EnumType_GeneratesStringEnum | NAK-510 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_ProducesDiagnosticsForInvalidTypes | Scanner_ProducesDiagnosticsForInvalidTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_CapabilityWithValidTypes_NoDiagnostics | Scanner_CapabilityWithValidTypes_NoDiagnostics | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_ListProperty_GeneratesGetterOnlyMethods | Generate_ListProperty_GeneratesGetterOnlyMethods | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_ListProperty_DoesNotUsePropertyObjectPattern | Generate_ListProperty_DoesNotUsePropertyObjectPattern | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_OptionalOptionsProperty_UsesDistinctOptionsBagParameter | Generate_OptionalOptionsProperty_UsesDistinctOptionsBagParameter | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_MutableCollectionProperties_UsePropertyAccessors | Generate_MutableCollectionProperties_UsePropertyAccessors | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_ConcreteAndInterfaceWithSameClassName_NoDuplicateClasses | Generate_ConcreteAndInterfaceWithSameClassName_NoDuplicateClasses | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_ResourceAndResourceNamedPromise_NoDuplicateDeclarations | Generate_ResourceAndResourceNamedPromise_NoDuplicateDeclarations | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Generate_SameMethodNameOnDifferentTypes_MergesOptionsInterface | Generate_SameMethodNameOnDifferentTypes_MergesOptionsInterface | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_WithNpm_ExpandsToAllJavaScriptResourceTypes | Scanner_WithNpm_ExpandsToAllJavaScriptResourceTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.Scanner_PackageManagerMethods_ExpandToAllJavaScriptResourceTypes | Scanner_PackageManagerMethods_ExpandToAllJavaScriptResourceTypes | NAK-509 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportWriterProducesFocusedCanonicalJson | ApiExportWriterProducesFocusedCanonicalJson | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportIncludesCodeGeneratorIdentity | ApiExportIncludesCodeGeneratorIdentity | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportIncludesExportedValuesWithGeneratedDeclaration | ApiExportIncludesExportedValuesWithGeneratedDeclaration | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportPromiseDeclarationContainsOnlySourcePromiseMembers | ApiExportOkTupleSpecContainsOnlySourcePromiseMembers | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiReferenceExporterRequiresAndHonorsCancellation | ApiReferenceExporterRequiresAndHonorsCancellation | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiReferenceExportOptionsCopiesExportingAssemblyNames | ApiReferenceExportOptionsCopiesExportingAssemblyNames | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiReferenceExportOptionsExposesReadOnlyExportingAssemblyNames | ApiReferenceExportOptionsExposesReadOnlyExportingAssemblyNames | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiReferenceExportOptionsRequiresAnExportingAssembly | ApiReferenceExportOptionsRequiresAnExportingAssembly | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiReferenceExportOptionsRequiresValidExportingAssemblyNames | ApiReferenceExportOptionsRequiresValidExportingAssemblyNames | NAK-511 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportEntrypointIdsIncludeTheOwningAssembly | ApiExportEntrypointIdsIncludeTheOwningAssembly | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportKeepsPackageLocalOptionsNamesAndCombinedGenerationIsDeterministic | ApiExportKeepsPackageLocalOptionsNamesAndCombinedGenerationIsDeterministic | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportExplicitInterfaceMemberNameMatchesItsDeclaration | ApiExportExplicitInterfaceMemberNameMatchesItsDeclaration | NAK-512 | planned |
| AtsTypeScriptCodeGeneratorTests.ApiExportUsesGeneratedSignaturesAndSeparatesReferencedTypes | ApiExportUsesGeneratedSignaturesAndSeparatesReferencedTypes | NAK-511 | planned |
| TypeScriptLanguageSupportTests.Scaffold_CreatesAppHostSpecificScriptsAndTsConfig_ForNewProject | Scaffold_CreatesAppHostSpecificScriptsAndMixExs_ForNewProject | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_BrownfieldOutput_ContainsOnlyAspireEntries | Scaffold_BrownfieldOutput_ContainsOnlyAspireEntries | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_NestedBrownfieldPackage_UsesStableAppHostPackageName | Scaffold_NestedBrownfieldPackage_UsesStableAppHostPackageName | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_AlwaysOutputsAspireVersions_RegardlessOfExistingDependencies | Scaffold_AlwaysOutputsAspireVersions_RegardlessOfExistingDependencies | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_DoesNotEmitRootTsConfig_WhenOneAlreadyExists | Scaffold_DoesNotEmitRootMixExs_WhenOneAlreadyExists | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_GeneratesProfilePortsOutsideWindowsEphemeralRange | Scaffold_GeneratesProfilePortsOutsideWindowsEphemeralRange | NAK-508 | planned |
| TypeScriptLanguageSupportTests.GetRuntimeSpec_UsesAppHostSpecificTsConfig | GetRuntimeSpec_UsesAppHostSpecificMixExs | NAK-508 | planned |
| TypeScriptLanguageSupportTests.SetCertificateBundleEnvironmentVariableIfSupported_IgnoresLegacyRuntimeSpec | SetCertificateBundleEnvironmentVariableIfSupported_IgnoresLegacyRuntimeSpec | NAK-509 | planned |
| TypeScriptLanguageSupportTests.Scaffold_EmitsScaffoldedEslintConfigVerbatim | Scaffold_EmitsScaffoldedFormatterExsVerbatim | NAK-508 | planned |
| TypeScriptLanguageSupportTests.Scaffold_EmitsScaffoldedAppHostTsConfigVerbatim | Scaffold_EmitsScaffoldedAppHostMixExsVerbatim | NAK-508 | planned |

## TypeScript JsTests (vitest)

M2 source: `tests/Aspire.Hosting.CodeGeneration.TypeScript.JsTests` (vitest). Target: `tests/Aspire.Hosting.CodeGeneration.Elixir.ExTests` (ExUnit). This source project has about 149 individual vitest `it` cases. Each row below groups the `it` cases under one enclosing `describe` block, instead of listing every case. Each row maps to one ExUnit `describe` block. That block must cover the same behavior as the source `describe` block.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| base.test.ts :: describe('ReferenceExpression') (15 tests) | describe "OkTuple / ReferenceExpression struct: format + value-provider construction, handle mode, conditional mode" | NAK-509 | planned |
| base.test.ts :: describe('create (tagged template)') (8 tests) | describe "the `create` sigil/macro building a reference expression from an interpolated string" | NAK-509 | planned |
| base.test.ts :: describe('extractHandleForExpr (tested via create)') (4 tests) | describe "extracting a Handle struct from an interpolated value inside the reference-expression builder" | NAK-509 | planned |
| base.test.ts :: describe('refExpr') (2 tests) | describe "the `refExpr` sigil/macro tagged-template helper" | NAK-509 | planned |
| base.test.ts :: describe('ResourceBuilderBase') (1 test) | describe "the base resource-builder struct's handle serialization" | NAK-509 | planned |
| base.test.ts :: describe('AspireList') (11 tests) | describe "the AspireList collection wrapper (count/get/add/removeAt/clear/toArray + lazy handle resolution)" | NAK-510 | planned |
| base.test.ts :: describe('AspireDict') (13 tests) | describe "the AspireDict collection wrapper (count/get/set/containsKey/remove/clear/keys/values/toObject + lazy handle resolution)" | NAK-510 | planned |
| eslint-config.test.ts :: describe('scaffolded eslint.config.mjs') (8 tests) | describe "the scaffolded .formatter.exs / Credo config that flags an unhandled `{:ok, _}` or unawaited Task from an AppHost pipeline" | NAK-508 | planned |
| nullableProperties.test.ts :: describe('generated nullable scalar property accessors') (3 tests) | describe "generated nullable scalar getter/setter typespecs and nil handling" | NAK-510 | planned |
| transport.test.ts :: describe('isAtsError') (6 tests) | describe "the ATS error-shape guard on a decoded transport message" | NAK-509 | planned |
| transport.test.ts :: describe('isMarshalledHandle') (7 tests) | describe "the marshalled-handle-shape guard on a decoded transport message" | NAK-509 | planned |
| transport.test.ts :: describe('AtsErrorCodes') (1 test) | describe "the ATS error code enum/constant module" | NAK-509 | planned |
| transport.test.ts :: describe('Handle') (4 tests) | describe "the Handle struct: construction, JSON serialization, and round-trip" | NAK-509 | planned |
| transport.test.ts :: describe('CancellationToken') (4 tests) | describe "the CancellationToken struct: construction from a remote token ID or an Elixir cancellation signal" | NAK-510 | planned |
| transport.test.ts :: describe('from') (2 tests) | describe "building a CancellationToken from an Elixir cancellation signal" | NAK-510 | planned |
| transport.test.ts :: describe('fromValue') (5 tests) | describe "coercing an arbitrary value into a CancellationToken" | NAK-510 | planned |
| transport.test.ts :: describe('register') (1 test) | describe "registering a CancellationToken and returning its remote token ID" | NAK-510 | planned |
| transport.test.ts :: describe('wrapIfHandle') (8 tests) | describe "wrapping a marshalled handle (including nested in lists/maps) into a Handle struct" | NAK-509 | planned |
| transport.test.ts :: describe('CapabilityError') (3 tests) | describe "the CapabilityError exception struct: name, code, and full error payload" | NAK-509 | planned |
| transport.test.ts :: describe('registerCallback / unregisterCallback / getCallbackCount') (3 tests) | describe "the callback registry: register, unregister, and count" | NAK-511 | planned |
| transport.test.ts :: describe('AppHostUsageError') (1 test) | describe "the AppHostUsageError exception struct" | NAK-509 | planned |
| transport.test.ts :: describe('circular reference detection in wrapIfHandle') (3 tests) | describe "circular-reference detection while wrapping handles nested in lists/maps/structs" | NAK-509 | planned |
| transport.test.ts :: describe('AspireClient') (9 tests) | describe "the AspireClient transport: connection state, ping, cancellation, capability invocation, and disconnect" | NAK-507 | planned |
| transport.test.ts :: describe('registerCancellation') (4 tests) | describe "registering and unregistering a CancellationToken's remote token ID with the transport" | NAK-507 | planned |
| transport.test.ts :: describe('registerHandleWrapper') (2 tests) | describe "registering a per-type Handle wrapper factory" | NAK-509 | planned |
| transport.test.ts :: describe('callback invocation protocol') (7 tests) | describe "the wire protocol for invoking a registered Elixir callback from the AppHost, including DTO writeback" | NAK-511 | planned |
| transport.test.ts :: describe('capability argument marshalling') (2 tests) | describe "marshalling DTO list properties into the JSON arguments sent to the AppHost server" | NAK-511 | planned |
| transport.test.ts :: describe('trackPromise / flushPendingPromises') (13 tests) | describe "tracking and flushing pending async Tasks so the AppHost process does not exit before they resolve" | NAK-507 | planned |

## CLI E2E Polyglot

M2 source: the `*Polyglot*` files in `tests/Aspire.Cli.EndToEnd.Tests` (`JavaPolyglotTests.cs`, `JavaPolyglotApphostDirectoryTests.cs`, `TypeScriptPolyglotTests.cs`, `TypeScriptPolyglotApphostDirectoryTests.cs`). Target: the matching Elixir polyglot end-to-end tests in the same project.

| Source test | Elixir test | Ticket | Status |
|---|---|---|---|
| JavaPolyglotApphostDirectoryTests.StopJavaPolyglotAppHostUsingApphostDirectory | StopElixirPolyglotAppHostUsingApphostDirectory | NAK-513 | planned |
| JavaPolyglotTests.CreateJavaAppHostWithViteApp | CreateElixirAppHostWithPhoenixApp | NAK-513 | planned |
| TypeScriptPolyglotApphostDirectoryTests.StopTypeScriptPolyglotAppHostUsingApphostDirectory | StopElixirPolyglotAppHostUsingApphostDirectory | NAK-513 | planned |
| TypeScriptPolyglotTests.CreateTypeScriptAppHostWithViteApp_UsesConfiguredToolchain | CreateElixirAppHostWithPhoenixApp_UsesConfiguredToolchain | NAK-513 | planned |
| TypeScriptPolyglotTests.GeneratedAspireDevScript_StartsWatchMode_WithConfiguredToolchain | GeneratedAspireDevScript_StartsWatchMode_WithConfiguredToolchain | NAK-514 | planned |
| TypeScriptPolyglotTests.InitTypeScriptAppHost_AugmentsExistingViteRepoInWorkspaceSubdirectory | InitElixirAppHost_AugmentsExistingPhoenixRepoInWorkspaceSubdirectory | NAK-513 | planned |
| TypeScriptPolyglotTests.TypeScriptAppHostWithVite_AllowsDifferentGuestPkgManager | n/a — Elixir has one canonical package manager (Hex via mix); there is no alternate guest package manager to allow | NAK-513 | n/a |

