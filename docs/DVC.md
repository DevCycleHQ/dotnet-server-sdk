# DevCycle.Api.DevcycleApi

All URIs are relative to *https://bucketing-api.devcycle.com/*

Method | HTTP request | Description
------------- | ------------- | -------------
[**GetFeaturesAsync**](DVC.md#getfeatures) | **POST** /v1/features | Get all features by key for user data
[**GetVariableByKeyAsync**](DVC.md#getvariablebykey) | **POST** /v1/variables/{key} | Get variable by key for user data
[**GetVariablesAsync**](DVC.md#getvariables) | **POST** /v1/variables | Get all variables by key for user data
[**TrackAsync**](DVC.md#track) | **POST** /v1/track | Post events to DevCycle for user

<a name="getfeatures"></a>
# **GetFeatures**
> Dictionary<string, Feature> GetFeatures (UserData body)

Get all features by key for user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class GetFeaturesExample
    {
        public async Task main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

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

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **user** | [**User**](User.md)|  | 

### Return type

[**Dictionary<string, Feature>**](Feature.md)

### Authorization

Server API Key

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a name="getvariablebykey"></a>
# **GetVariableByKey**
> Variable GetVariableByKey (UserData body, string key)

Get variable by key for user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class GetVariableByKeyExample
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                var key = "YOUR_KEY";
                Variable result = await api.GetVariableByKeyAsync(user, key);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.GetFeaturesByKeyAsync: " + e.Message );
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

<a name="getvariables"></a>
# **GetVariables**
> Dictionary<string, Variable> GetVariables (UserData body)

Get all variables by key for user data

### Example
```csharp
using System;
using System.Diagnostics;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    public class GetVariablesExample
    {
        public void main()
        {
            // Ensure REST Client resources are correctly disposed once no longer required
            using DVC api = new DVC("YOUR_API_KEY");
            var user = new User("user_id"); 

            try
            {
                Dictionary<string, Variable> result = await api.GetVariablesAsync(user);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling DVC.GetVariablesAsync: " + e.Message );
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
