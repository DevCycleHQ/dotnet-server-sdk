using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public partial class UserAndEvents : IEquatable<UserAndEvents>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAndEvents" /> class.
        /// </summary>
        /// <param name="events">events.</param>
        /// <param name="user">user.</param>
        public UserAndEvents(List<DevCycleEvent> events = default, DevCycleUser user = default)
        {
            this.Events = events;
            this.User = user;
        }
        
        /// <summary>
        /// Gets or Sets Events
        /// </summary>
        [DataMember(Name="events", EmitDefaultValue=false)]
        [JsonProperty("events")]
        public List<DevCycleEvent> Events { get; set; }

        /// <summary>
        /// Gets or Sets User
        /// </summary>
        [DataMember(Name="user", EmitDefaultValue=false)]
        [JsonProperty("user")]
        public DevCycleUser User { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserAndEvents {\n");
            sb.Append("  Events: ").Append(Events).Append("\n");
            sb.Append("  User: ").Append(User).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as UserAndEvents);
        }

        /// <summary>
        /// Returns true if UserDataAndEventsBody instances are equal
        /// </summary>
        /// <param name="input">Instance of UserDataAndEventsBody to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UserAndEvents input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Events == input.Events ||
                    this.Events != null &&
                    input.Events != null &&
                    this.Events.SequenceEqual(input.Events)
                ) && 
                (
                    this.User == input.User ||
                    (this.User != null &&
                    this.User.Equals(input.User))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Events != null)
                    hashCode = hashCode * 59 + this.Events.GetHashCode();
                if (this.User != null)
                    hashCode = hashCode * 59 + this.User.GetHashCode();
                return hashCode;
            }
        }
    }
}