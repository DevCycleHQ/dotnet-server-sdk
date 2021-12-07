# DevCycle - the dotnet library for the DevCycle Bucketing API

Documents the DevCycle Bucketing API which provides and API interface to User Bucketing and for generated SDKs.


<a name="frameworks-supported"></a>
## Frameworks supported
- .NET Core >=1.0
- .NET Framework >=4.6
- Mono/Xamarin >=vNext
- UWP >=10.0

<a name="dependencies"></a>
## Dependencies
- FubarCoder.RestSharp.Portable.Core >=4.0.8
- FubarCoder.RestSharp.Portable.HttpClient >=4.0.8
- JsonSubTypes >=1.8.0
- Newtonsoft.Json >=13.0.1

<a name="installation"></a>
## Installation
Download the SDK from Nuget - https://nuget.info/packages/DevCycle.DotNet.Server.SDK/1.0.1
and use the namespaces:
```csharp
using DevCycle.Api;
using DevCycle.Client;
using DevCycle.Model;
```
<a name="getting-started"></a>
## Getting Started

```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Client;
using DevCycle.Model;

namespace Example
{
    public class Example
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVCClient api = new DVCClient("YOUR_API_KEY");
            var user = new User("a_user_id"); 

            try
            {
                Dictionary<string, Feature> result = await api.AllFeaturesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVCClient.AllFeaturesAsync: " + e.Message );
            }
        }
    }
}
```

<a name="documentation-for-api-endpoints"></a>
## Documentation for API Endpoints

Class | Method | HTTP request | Description
------------ | ------------- | ------------- | -------------
*DVC* | [**AllFeaturesAsync**](docs/DVC.md#getfeatures) | **POST** /v1/features | Get all features for user
*DVC* | [**VariableAsync**](docs/DVC.md#getvariablebykey) | **POST** /v1/variables/{key} | Get variable by key for user
*DVC* | [**AllVariablesAsync**](docs/DVC.md#getvariables) | **POST** /v1/variables | Get all variables for user
*DVC* | [**TrackAsync**](docs/DVC.md#track) | **POST** /v1/track | Post events to DevCycle for user

<a name="documentation-for-models"></a>
## Documentation for Models

 - [Model.ErrorResponse](docs/ErrorResponse.md)
 - [Model.Feature](docs/Feature.md)
 - [Model.DVCResponse](docs/DVCResponse.md)
 - [Model.Event](docs/Event.md)
 - [Model.User](docs/User.md)
 - [Model.UserDataAndEventsBody](docs/UserAndEvents.md)
 - [Model.Variable](docs/Variable.md)