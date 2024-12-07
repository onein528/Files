# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$Branch = "", # This has to correspond with one of the AppEnvironment enum values
    [string]$SolutionPath = ""
    [string]$PackageManifestPath = "",
    [string]$Publisher = "",
    [string]$WorkingDir = "",
    [string]$SecretBingMapsKey = "",
    [string]$SecretSentry = "",
    [string]$SecretGitHubOAuthClientId = ""
)

msbuild $SolutionPath `
  /t:Restore `
  /p:Platform=$env:PLATFORM `
  /p:Configuration=$env:CONFIGURATION `
  /p:PublishReadyToRun=true

if ($Branch -eq "Dev")
{
    msbuild $SolutionPath `
      /t:Build `
      /p:Platform=$env:PLATFORM `
      /p:Configuration=$env:CONFIGURATION `
      /p:AppxBundlePlatforms=$env:APPX_BUNDLE_PLATFORMS `
      /p:AppxPackageDir=$env:APPX_PACKAGE_DIR `
      /p:AppxBundle=Always `
      /p:UapAppxPackageBuildMode=Sideload `
      /p:GenerateAppInstallerFile=True `
      /p:AppInstallerUri=$env:APP_INSTALLER_SIDELOAD_URL
}
