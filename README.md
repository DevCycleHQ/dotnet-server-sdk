# DevCycle .NET Server SDK

Welcome to the DevCycle .NET Server SDK, which interfaces with a local bucketing library. This SDK requests config from DevCycle servers on DevCycleClient initialization. 
All calls to the client will then perform local bucketing to determine if a user receives a specific variation.
Events are queued and flushed periodically in the background.
This version is compatible with .NET Standard 2.0 and utilizes more resources to perform local bucketing.

## Installation
Download the SDK from Nuget - https://www.nuget.org/packages/DevCycle.SDK.Server.Local/

## Getting Started
Use the example app `DevCycle.SDK.Server.Local.Example`. It will read your DevCycle SDK key from an environment variable `DEVCYCLE_SERVER_SDK_KEY`

Your DevCycle SDK key can be found via [Environments & Keys Settings](https://www.devcycle.com/r/environments) on the DevCycle dashboard.

## Usage
To find usage documentation, visit our docs for [Local Bucketing](https://docs.devcycle.com/docs/sdk/server-side-sdks/dotnet-local).