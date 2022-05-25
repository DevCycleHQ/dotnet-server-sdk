using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

internal class UserAndFeatureVars
{
    public readonly DVCPopulatedUser User;
    private readonly Dictionary<string, string> featureVars;

    public UserAndFeatureVars(DVCPopulatedUser user, Dictionary<string, string> featureVars)
    {
        User = user;
        this.featureVars = featureVars;
    }

    private int FeatureVarsHashCode()
    {
        var sum = 0;
        foreach (var entry in featureVars)
        {
            sum += entry.Key.GetHashCode();
            sum += entry.Value.GetHashCode();
        }

        return sum;
    }

    public override int GetHashCode()
    {
        return User.GetHashCode() + FeatureVarsHashCode();
    }

    public override bool Equals(object obj)
    {
        return GetHashCode().Equals(obj?.GetHashCode());
    }
}