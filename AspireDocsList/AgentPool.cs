using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

public class AgentPool(ILogger<AgentPool> logger)
{
    private readonly Dictionary<string, AIAgent> _agents = new();

    public void AddAgent(string key, AIAgent agent)
    {
        _agents[key] = agent;
        logger.LogInformation($"Agent added with key: {key}");
    }

    public bool RemoveAgent(string key)
    {
        if (_agents.ContainsKey(key))
        {
            var removed = _agents.Remove(key);
            logger.LogInformation($"Agent removed with key: {key}");
            return removed;
        }
        logger.LogWarning($"Attempted to remove agent with key: {key}, but it was not found.");
        return false;
    }

    public AIAgent? GetAgent(string key)
    {
        if (_agents.TryGetValue(key, out var agent))
        {
            logger.LogInformation($"Agent retrieved with key: {key}");
            return agent;
        }
        logger.LogWarning($"Agent with key: {key} not found.");
        return null;
    }

    public IEnumerable<KeyValuePair<string, AIAgent>> GetAllAgents()
    {
        logger.LogInformation($"Retrieving all agents. Count: {_agents.Count}");
        return _agents;
    }

    public IEnumerable<string> Keys => _agents.Keys;
    public IEnumerable<AIAgent> Agents => _agents.Values;
}