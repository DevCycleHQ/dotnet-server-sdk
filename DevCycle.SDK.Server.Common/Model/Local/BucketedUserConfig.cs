using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class BucketedUserConfig
    {
        [DataMember(Name="project", EmitDefaultValue=false)]
        public Project Project { get; set; }
        
        [DataMember(Name="environment", EmitDefaultValue=false)]
        public Environment Environment { get; set; }
        
        [DataMember(Name="features", EmitDefaultValue=false)]
        public Dictionary<string, Feature> Features { get; set; }
        
        [DataMember(Name="featureVariationMap", EmitDefaultValue=false)]
        public Dictionary<string, string> FeatureVariationMap { get; set; }

        [DataMember(Name="variableVariationMap", EmitDefaultValue=false)]
        public Dictionary<string, FeatureVariation> VariableVariationMap { get; set; }
        
        [DataMember(Name="variables", EmitDefaultValue=false)]
        public Dictionary<string, ReadOnlyVariable<object>> InternalVariables { get; set; }
        public VariableCollection Variables { get; private set; }
        
        [DataMember(Name="knownVariableKeys", EmitDefaultValue=false)]
        public List<decimal> KnownVariableKeys { get; set; }
        
        public void InitializeVariables()
        {
            Variables = new VariableCollection(InternalVariables);
            if (FeatureVariationMap == null)
            {
                FeatureVariationMap = new Dictionary<string, string>();
            }
        }
    }
}