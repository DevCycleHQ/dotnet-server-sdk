# DevCycle .NET Cloud Server SDK

Welcome to the DevCycle .NET Cloud Server SDK. All calls to the client will send requests to DevCycle for user-specific configuration to to determine if a user receives a specific variation.
Events are queued and flushed periodically in the background.
This version uses .NET Standard 2.0 and relies on the DevCycle backend to perform bucketing.

## Installation
Download the SDK from Nuget - https://www.nuget.org/packages/DevCycle.DotNet.Server.Local.SDK/1.3.5

## Getting Started
Use the example app `DevCycle.SDK.Server.Cloud.Example`. It will read your DevCycle SDK key from an environment variable `DEVCYCLE_SDK_TOKEN`

Your DevCycle SDK key can be found via [Environments & Keys Settings](https://www.devcycle.com/r/environments) on the DevCycle dashboard.

## Usage
To find usage documentation, visit our docs for [Cloud Bucketing](https://docs.devcycle.com/docs/sdk/server-side-sdks/dotnet-cloud)