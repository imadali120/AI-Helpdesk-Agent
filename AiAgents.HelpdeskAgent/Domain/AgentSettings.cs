namespace AiAgents.HelpdeskAgent.Domain;

public class AgentSettings
{
    public int Id { get; set; }
    public double ConfidenceThreshold { get; set; }
    public bool EnableAutoAssign { get; set; }
    public bool EnableAutoAskClarifyingQuestions { get; set; }
}

