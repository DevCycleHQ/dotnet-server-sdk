using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

public class CLocalBucketing : ILocalBucketing
{
    public string ClientUUID { get; }
    
    public CLocalBucketing()
    {
        ClientUUID = System.Guid.NewGuid().ToString();
    }
    
    public void InitEventQueue(string sdkKey, string options)
    {
        throw new System.NotImplementedException();
    }

    public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user)
    {
        throw new System.NotImplementedException();
    }

    public int EventQueueSize(string sdkKey)
    {
        throw new System.NotImplementedException();
    }

    public void QueueEvent(string sdkKey, string user, string eventString)
    {
        throw new System.NotImplementedException();
    }

    public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr)
    {
        throw new System.NotImplementedException();
    }

    public List<FlushPayload> FlushEventQueue(string sdkKey)
    {
        throw new System.NotImplementedException();
    }

    public void OnPayloadSuccess(string sdkKey, string payloadId)
    {
        throw new System.NotImplementedException();
    }

    public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable)
    {
        throw new System.NotImplementedException();
    }

    public void StoreConfig(string sdkKey, string config)
    {
        throw new System.NotImplementedException();
    }

    public void SetPlatformData(string platformData)
    {
        throw new System.NotImplementedException();
    }

    public string GetVariable(string sdkKey, string userJSON, string key, TypeEnum variableType, bool shouldTrackEvent)
    {
        throw new System.NotImplementedException();
    }

    public byte[] GetVariable(byte[] serializedParams)
    {
        throw new System.NotImplementedException();
    }

    public string GetConfigMetadata(string sdkKey)
    {
        throw new System.NotImplementedException();
    }

    public void SetClientCustomData(string sdkKey, string customData)
    {
        throw new System.NotImplementedException();
    }

    public void StartFlush()
    {
        throw new System.NotImplementedException();
    }

    public void EndFlush()
    {
        throw new System.NotImplementedException();
    }
}