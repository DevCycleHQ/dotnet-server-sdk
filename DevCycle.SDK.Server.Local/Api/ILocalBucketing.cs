using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

public interface ILocalBucketing
{
    public void InitEventQueue(string envKey, string options);
    public List<FlushPayload> FlushEventQueue(string envKey);
    public void OnPayloadSuccess(string envKey, string payloadId);
    public void OnPayloadFailure(string envKey, string payloadId, bool retryable);
    public BucketedUserConfig GenerateBucketedConfig(string token, string user);
    public int EventQueueSize(string envKey);
    public void QueueEvent(string envKey, string user, string eventString);
    public void QueueAggregateEvent(string envKey, string user, string eventString);
    public void StoreConfig(string token, string config);
    public void SetPlatformData(string platformData);

}