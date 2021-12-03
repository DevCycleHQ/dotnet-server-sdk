# DevCycle.DotNet.Server.SDK.Api.DVCClient

All URIs are relative to *https://bucketing-api.devcycle.com/*

Method | HTTP request | Description
------------- | ------------- | -------------
[**AllFeaturesAsync**](DVCClient.md#allFeatures) | **POST** /v1/features | Get all features by key for user data
[**VariableAsync**](DVCClient.md#variable) | **POST** /v1/variables/{key} | Get variable by key for user data
[**AllVariablesAsync**](DVCClient.md#allVariables) | **POST** /v1/variables | Get all variables by key for user data
[**TrackAsync**](DVCClient.md#track) | **POST** /v1/track | Post events to DevCycle for user

<a name="allFeatures"></a>
# **AllFeatures**
> Dictionary<string, Feature> AllFeatures (User user)

Get all features by user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class AllFeaturesExample
    {
        public async Task main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVCClient dvcClient = new DVCClient("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                Dictionary<string, Feature> result = await dvcClient.AllFeaturesAsync(user);
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

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **user** | [**User**](User.md)|  | 

### Return type

[**Dictionary<string, Feature>**](Feature.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a name="variable"></a>
# **Variable**
> Variable Variable (User body, string key, T defaultValue)

Get variable by key for user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class VariableExample
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVCClient dvcClient = new DVCClient("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                var key = "YOUR_KEY";
                Variable result = await dvcClient.VariableAsync(user, key);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVCClient.VariableAsync: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **user** | [**User**](User.md)|  | 
 **key** | **string**| Variable key | 

### Return type

[**Variable**](Variable.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a name="allVariables"></a>
# **AllVariables**
> Dictionary<string, Variable> AllVariables (User user)

Get all variables by key for user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class AllVariablesExample
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVCClient dvcClient = new DVCClient("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                Dictionary<string, Variable> result = await dvcClient.AllVariablesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVCClient.AllVariablesAsync: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **user** | [**User**](User.md)|  | 

### Return type

[**Dictionary<string, Variable>**](Variable.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a name="track"></a>
# **Track**
> DVCResponse TrackAsync (User user, Event event)

Post events to DevCycle for user

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class TrackExample
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVCClient dvcClient = new DVCClient("YOUR_API_KEY");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            
            var user = new Users("user_id");
            var events = new List<Event>();
            events.Add(new Event("test event", "test target", unixTimeMilliseconds, 600));
            var userAndEvents = new UserAndEvents(events, user); 

            try
            {
                DVCResponse result = await dvcClient.TrackAsync(userAndEvents);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVCClient.GetFeaturesAsync: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **body** | [**UserAndEvents**](UserAndEvents.md)|  | 

### Return type

[**DVCResponse**](DVCResponse.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)
