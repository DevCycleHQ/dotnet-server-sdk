# DevCycle.Api.DevcycleApi

All URIs are relative to *https://bucketing-api.devcycle.com/*

Method | HTTP request | Description
------------- | ------------- | -------------
[**AllFeaturesAsync**](DVC.md#allFeatures) | **POST** /v1/features | Get all features by key for user data
[**VariableAsync**](DVC.md#variable) | **POST** /v1/variables/{key} | Get variable by key for user data
[**AllVariablesAsync**](DVC.md#allVariables) | **POST** /v1/variables | Get all variables by key for user data
[**TrackAsync**](DVC.md#track) | **POST** /v1/track | Post events to DevCycle for user

<a name="allFeatures"></a>
# **AllFeatures**
> Dictionary<string, Feature> AllFeatures (UserData body)

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
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                Dictionary<string, Feature> result = await api.AllFeaturesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.AllFeaturesAsync: " + e.Message );
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
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                var key = "YOUR_KEY";
                Variable result = await api.VariableAsync(user, key);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.VariableAsync: " + e.Message );
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
> Dictionary<string, Variable> AllVariables (User body)

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
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                Dictionary<string, Variable> result = await api.AllVariablesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.AllVariablesAsync: " + e.Message );
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
> DVCResponse TrackAsync (UserAndEvents user)

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
            using DVC api = new DVC("YOUR_API_KEY");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            
            var user = new Users("user_id");
            var events = new List<Event>();
            events.Add(new Event("test event", "test target", unixTimeMilliseconds, 600));
            var userAndEvents = new UserAndEvents(events, user); 

            try
            {
                DVCResponse result = await api.TrackAsync(userAndEvents);
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

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **body** | [**UserAndEvents**](UserAndEvents.md)|  | 

### Return type

[**DVCResponse**](DVCResponse.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)
