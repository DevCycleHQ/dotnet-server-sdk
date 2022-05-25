using System.Collections.Generic;
using System.Linq;
using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Local.Api;

internal class AggregateEventQueues
{
    private readonly Dictionary<
        UserAndFeatureVars,
        Dictionary<string, DVCRequestEvent>> eventQueueMap;
        
    public AggregateEventQueues()
    {
        eventQueueMap = new Dictionary<UserAndFeatureVars, Dictionary<string, DVCRequestEvent>>();
    }

    public void AddEvent(UserAndFeatureVars userFeatureVars, DVCRequestEvent requestEvent)
    {
        if (!eventQueueMap.ContainsKey(userFeatureVars))
        {
            eventQueueMap[userFeatureVars] = new Dictionary<string, DVCRequestEvent>();
        }

        var eventKey = GetEventMapKey(requestEvent);

        if (!eventQueueMap[userFeatureVars].ContainsKey(eventKey))
        {
            eventQueueMap[userFeatureVars][eventKey] = requestEvent;
        }
        else
        {
            eventQueueMap[userFeatureVars][eventKey].Value += 1;
        }
    }

    private string GetEventMapKey(DVCRequestEvent requestEvent)
    {
        return requestEvent.Type + requestEvent.Target;
    }

    public Dictionary<DVCPopulatedUser, UserEventsBatchRecord> GetEventBatches()
    {
        // regroup aggregate events into batches by unique user
        var userEventBatches = new Dictionary<DVCPopulatedUser, UserEventsBatchRecord>();
            
        foreach (var entries in eventQueueMap)
        {
            var user = entries.Key.User;
            if (!userEventBatches.ContainsKey(user))
            {
                userEventBatches[user] = new UserEventsBatchRecord(user);
            }
                
            userEventBatches[user].Events.AddRange(entries.Value.Values.ToList());
        }
            
        return userEventBatches;
    }

    public void Clear()
    {
        eventQueueMap.Clear();
    }
}