# DevCycle.Model.Event
## Properties

| Name         | Type       | Description                                                      | Notes      |
|--------------|------------|------------------------------------------------------------------|------------|
| **Type**     | **string** | Custom event type                                                |            |
| **Target**   | **string** | Custom event target / subject of event. Contextual to event type | [optional] |
| **Date**     | **long?**  | Unix epoch time the event occurred according to client           | [optional] |
| **Value**    | **long?**  | Value for numerical events. Contextual to event type             | [optional] |
| **MetaData** | **Object** | Extra JSON metadata for event. Contextual to event type          | [optional] |

[[Back to README]](../README.md)

