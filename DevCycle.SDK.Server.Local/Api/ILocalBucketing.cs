using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

public interface ILocalBucketing
{
    public string ClientUUID { get; }
    public void InitEventQueue(string sdkKey, string options);
    public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user);
    public int EventQueueSize(string sdkKey);
    public void QueueEvent(string sdkKey, string user, string eventString);
    public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr);
    public List<FlushPayload> FlushEventQueue(string sdkKey);
    public void OnPayloadSuccess(string sdkKey, string payloadId);
    public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable);

    public void StoreConfig(string sdkKey, string config);
    public void SetPlatformData(string platformData);
    public string GetVariable(string sdkKey, string userJSON, string key, TypeEnum variableType, bool shouldTrackEvent);
    public string GetConfigMetadata(string sdkKey);
    public byte[] GetVariableForUserProtobuf(byte[] serializedParams);
    public void SetClientCustomData(string sdkKey, string customData);
    public void StartFlush();
    public void EndFlush();

}