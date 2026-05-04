namespace TheUnlocker.Automation;

public sealed class WorkflowRule
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "";
    public string Trigger { get; init; } = "";
    public string Condition { get; init; } = "";
    public string[] Actions { get; init; } = [];
    public bool Enabled { get; init; } = true;
}

public sealed class WorkflowEvaluation
{
    public string RuleId { get; init; } = "";
    public bool Matched { get; init; }
    public string[] Actions { get; init; } = [];
    public string Reason { get; init; } = "";
}

public sealed class WorkflowAutomationEngine
{
    public WorkflowEvaluation Evaluate(WorkflowRule rule, string trigger, IReadOnlyDictionary<string, string> facts)
    {
        if (!rule.Enabled || !rule.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase))
        {
            return new WorkflowEvaluation { RuleId = rule.Id, Matched = false, Reason = "Trigger did not match." };
        }

        var matched = string.IsNullOrWhiteSpace(rule.Condition) ||
            facts.Any(fact => rule.Condition.Contains($"{fact.Key}={fact.Value}", StringComparison.OrdinalIgnoreCase));

        return new WorkflowEvaluation
        {
            RuleId = rule.Id,
            Matched = matched,
            Actions = matched ? rule.Actions : [],
            Reason = matched ? "Rule matched." : "Condition did not match."
        };
    }
}
