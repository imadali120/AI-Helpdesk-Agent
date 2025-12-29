namespace AiAgents.Core;

public abstract class SoftwareAgent<TPercept, TAction, TResult>
{
    public abstract Task<TResult?> StepAsync(CancellationToken cancellationToken = default);
}

