# DevCycle - the C# library for the DevCycle Bucketing API

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
Generate the DLL using your preferred tool

Then include the DLL (under the `bin` folder) in the C# project, and use the namespaces:
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
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("a_user_id"); 

            try
            {
                Dictionary<string, Feature> result = await api.GetFeaturesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.GetFeaturesAsync: " + e.Message );
            }
        }
    }
}
```

<a name="documentation-for-api-endpoints"></a>
## Documentation for API Endpoints

Class | Method | HTTP request | Description
------------ | ------------- | ------------- | -------------
*DVC* | [**GetFeaturesAsync**](docs/DVC.md#getfeatures) | **POST** /v1/features | Get all features for user
*DVC* | [**GetVariableByKeyAsync**](docs/DVC.md#getvariablebykey) | **POST** /v1/variables/{key} | Get variable by key for user
*DVC* | [**GetVariablesAsync**](docs/DVC.md#getvariables) | **POST** /v1/variables | Get all variables for user
*DVC* | [**TrackAsync**](docs/DVC.md#postevents) | **POST** /v1/track | Post events to DevCycle for user

<a name="documentation-for-models"></a>
## Documentation for Models

 - [Model.ErrorResponse](docs/ErrorResponse.md)
 - [Model.Feature](docs/Feature.md)
 - [Model.InlineResponse201](docs/InlineResponse201.md)
 - [Model.ModelEvent](docs/ModelEvent.md)
 - [Model.UserData](docs/UserData.md)
 - [Model.UserDataAndEventsBody](docs/UserDataAndEventsBody.md)
 - [Model.Variable](docs/Variable.md)

<a name="documentation-for-authorization"></a>
## Documentation for Authorization

<a name="bearerAuth"></a>
### bearerAuth

- **Type**: API key
- **API key parameter name**: Authorization
- **Location**: HTTP header

