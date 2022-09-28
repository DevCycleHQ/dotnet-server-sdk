# DevCycle.Model.User
## Properties

| Name                  | Type       | Description                                                                                                           | Notes      |
|-----------------------|------------|-----------------------------------------------------------------------------------------------------------------------|------------|
| **UserId**            | **string** | Unique id to identify the user                                                                                        |            |
| **Email**             | **string** | User&#x27;s email used to identify the user on the dashboard / target audiences                                       | [optional] |
| **Name**              | **string** | User&#x27;s name used to identify the user on the dashboard / target audiences                                        | [optional] |
| **Language**          | **string** | User&#x27;s language in ISO 639-1 format                                                                              | [optional] |
| **Country**           | **string** | User&#x27;s country in ISO 3166 alpha-2 format                                                                        | [optional] |
| **AppVersion**        | **string** | App Version of the running application                                                                                | [optional] |
| **AppBuild**          | **double** | App Build number of the running application                                                                           | [optional] |
| **CustomData**        | **Object** | User&#x27;s custom data to target the user with, data will be logged to DevCycle for use in dashboard.                | [optional] |
| **PrivateCustomData** | **Object** | User&#x27;s custom data to target the user with, data will not be logged to DevCycle only used for feature bucketing. | [optional] |
| **CreatedDate**       | **DateTime**  | Date the user was created                                                                | [optional] |
| **LastSeenDate**      | **DateTime*   | Date the user was created                                                                | [optional] |
| **Platform**          | **string** | Platform the Client SDK is running on                                                                                 | [optional] |
| **PlatformVersion**   | **string** | Version of the platform the Client SDK is running on                                                                  | [optional] |
| **DeviceModel**       | **string** | User&#x27;s device model                                                                                              | [optional] |
| **SdkType**           | **string** | DevCycle SDK type                                                                                                     | [optional] |
| **SdkVersion**        | **string** | DevCycle SDK Version                                                                                                  | [optional] |

[[Back to README]](../README.md)

