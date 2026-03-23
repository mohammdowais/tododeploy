# TodoDeploy

TodoDeploy is a WinUI 3 desktop app. This repo includes:

- the app UI
- a GitHub Actions release workflow
- GitHub Releases based in-app updates through Velopack

## Requirements

- Windows
- .NET 8 SDK
- WinUI 3 build environment / Windows App SDK support

## Build and run from CLI

### Restore

```powershell
dotnet restore .\tododeploy.csproj
```

### Build

```powershell
dotnet build .\tododeploy.csproj -p:Platform=x64
```

### Run a local development build

This project can be built normally from the CLI:

```powershell
dotnet build .\tododeploy.csproj -p:Platform=x64
```

A plain debug build is mainly for development and validation. For a release-like local run, use the unpackaged publish path below.

### Publish and run the same style of build used for releases

This is the recommended CLI path if you want to test the installer/update-ready app layout locally.

```powershell
dotnet publish .\tododeploy.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\artifacts\publish-local `
  -p:WindowsPackageType=None `
  -p:AppxPackage=false
```

Run the published app:

```powershell
.\artifacts\publish-local\tododeploy.exe
```

## In-app update check

The app has a `Check for updates` button in the UI.

How it works:

- it checks GitHub Releases from `https://github.com/mohammdowais/tododeploy`
- if a newer release exists, it downloads the update
- it prompts the user to install and restart

Important:

- update checks only work from a Velopack-installed release build
- they do not work from a normal Visual Studio / loose debug run
- install the app from a GitHub Release first, then use the update button

## GitHub Actions release flow

The workflow is at:

- `.github/workflows/release.yml`

What it does on a version tag:

1. checks out the repo
2. restores dependencies
3. publishes an unpackaged `win-x64` build
4. creates a Velopack installer
5. uploads the installer and update feed assets to GitHub Releases

The workflow triggers on tags matching:

```text
v*
```

Example tags:

- `v0.1.0`
- `v0.2.3`

## How to trigger a release

### Option 1: commit current changes, tag, and push

```powershell
git add .
git commit -m "Prepare release v0.1.0"
git push origin main
git tag v0.1.0
git push origin v0.1.0
```

### Option 2: create an annotated tag after your commit is already pushed

```powershell
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin v0.1.0
```

After the tag is pushed, GitHub Actions will create or update the matching GitHub Release and upload the installer/update artifacts.

## Versioning notes

- keep release tags in `vX.Y.Z` format
- the workflow strips the leading `v` and uses the remaining value as the app/package version
- users already installed from a GitHub Release can update to newer tagged releases from inside the app

## Useful CLI commands

Rebuild the project:

```powershell
dotnet build .\tododeploy.csproj -p:Platform=x64
```

Create a local unpackaged publish:

```powershell
dotnet publish .\tododeploy.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish-local -p:WindowsPackageType=None -p:AppxPackage=false
```

Check local tags:

```powershell
git tag
```

Push a new release tag:

```powershell
git push origin v0.1.0
```
