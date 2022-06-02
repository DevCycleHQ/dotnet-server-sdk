using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

public interface ILocalBucketing
{
    public BucketedUserConfig GenerateBucketedConfig(string token, string user);
    public void StoreConfig(string token, string config);
    public void SetPlatformData(string platformData);

}