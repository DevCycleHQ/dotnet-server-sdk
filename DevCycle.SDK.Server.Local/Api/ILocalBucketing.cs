using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

public interface ILocalBucketing
{
    public void InitEventQueue(string sdkKey, string options);
    public List<FlushPayload> FlushEventQueue(string sdkKey);
    public void OnPayloadSuccess(string sdkKey, string payloadId);
    public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable);
    public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user);
    public int EventQueueSize(string sdkKey);
    public void QueueEvent(string sdkKey, string user, string eventString);
    public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr);
    public void StoreConfig(string sdkKey, string config);
    public void SetPlatformData(string platformData);
    public void StartFlush();
    public void EndFlush();
}