# Changelog

All notable changes to Imageflow.Net are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Releases before v0.15.1 are
described in the [GitHub releases](https://github.com/imazen/imageflow-dotnet/releases).

## [Unreleased]

### QUEUED BREAKING CHANGES

- None.

### Changed

- Build tooling: Microsoft.SourceLink.GitHub 10.0.401 (clears the Microsoft.Build.Tasks.Git 8.0.0 advisory), a private asset; the package's dependency list is unchanged. PolySharp stays at 1.15.0 to match Imageflow.Test (1.16 in the library alone breaks the net481 test build) (8d21d12).
- Tests and benchmarks: MSTest 3.11.1 in the .NET Framework PackageReference tests, BenchmarkDotNet 0.15.8 (8d21d12).
- CI: checkout v7, cache v6, setup-dotnet v6, upload-artifact v7, download-artifact v8, codecov v7, check-version-format-action v5 (702519d).

### Known Bugs

- On main, 69 of 97 tests fail: the bindings require libimageflow ABI 3.2 but the test projects reference the ABI 3.1 native 2.2.0-rc01 packages. PR #71 moves them to 2.3.1-rc01.
