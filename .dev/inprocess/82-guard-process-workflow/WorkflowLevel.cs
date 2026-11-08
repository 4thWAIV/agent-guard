public enum ExecutionLevel
{
    L1 = 1,
    L2 = 2,
    L3 = 3
}

public enum ExecutionRequirement
{
    Required,
    Excluded,
    Absent
}

public abstract record WorkflowLevel
{
    public abstract ExecutionLevel Level { get; }

    public abstract ExecutionRequirement GroundStage { get; }
    public abstract ExecutionRequirement DesignStage { get; }
    public abstract ExecutionRequirement ContractStage { get; }
    public abstract ExecutionRequirement RuleStage { get; }
    public abstract ExecutionRequirement ArchitectureStage { get; }
    // When performed, this stage always includes the Test Reviewer.
    public abstract ExecutionRequirement TestStage { get; }
    public abstract ExecutionRequirement ImplementStage { get; }
    public abstract ExecutionRequirement ReadinessStage { get; }
    public abstract ExecutionRequirement RefuteStage { get; }
    public abstract ExecutionRequirement GateStage { get; }
    public abstract ExecutionRequirement ReportStage { get; }

    // Workflow reviewers; stage reviewers must be a subset of those enabled here.
    // The Test Reviewer's required presence in TEST is the exception.
    public abstract ExecutionRequirement ProveItReviewer { get; }
    public abstract ExecutionRequirement SolidReviewer { get; }
    public abstract ExecutionRequirement DryReviewer { get; }
    public abstract ExecutionRequirement LazinessReviewer { get; }
    public abstract ExecutionRequirement LieReviewer { get; }
}

public sealed record L1Workflow(bool SkipRuleStage) : WorkflowLevel
{
    public override ExecutionLevel Level => ExecutionLevel.L1;

    public override ExecutionRequirement GroundStage => ExecutionRequirement.Required;
    public override ExecutionRequirement DesignStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ContractStage => ExecutionRequirement.Required;
    public override ExecutionRequirement RuleStage => SkipRuleStage
        ? ExecutionRequirement.Excluded
        : ExecutionRequirement.Required;
    public override ExecutionRequirement ArchitectureStage => ExecutionRequirement.Required;
    public override ExecutionRequirement TestStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ImplementStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReadinessStage => ExecutionRequirement.Required;
    public override ExecutionRequirement RefuteStage => ExecutionRequirement.Required;
    public override ExecutionRequirement GateStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReportStage => ExecutionRequirement.Required;

    public override ExecutionRequirement ProveItReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement SolidReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement DryReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement LazinessReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement LieReviewer => ExecutionRequirement.Required;
}

public sealed record L2Workflow(
    bool NeedsDesign,
    bool NeedsArch,
    bool NeedsTest,
    bool NeedsLaziness) : WorkflowLevel
{
    public override ExecutionLevel Level => ExecutionLevel.L2;

    public override ExecutionRequirement GroundStage => ExecutionRequirement.Required;
    public override ExecutionRequirement DesignStage => NeedsDesign
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement ContractStage => ExecutionRequirement.Required;
    public override ExecutionRequirement RuleStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement ArchitectureStage => NeedsArch
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement TestStage => NeedsTest
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement ImplementStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReadinessStage => ExecutionRequirement.Required;
    public override ExecutionRequirement RefuteStage => ExecutionRequirement.Required;
    public override ExecutionRequirement GateStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReportStage => ExecutionRequirement.Required;

    public override ExecutionRequirement ProveItReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement SolidReviewer => ExecutionRequirement.Absent;
    public override ExecutionRequirement DryReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement LazinessReviewer => NeedsLaziness
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement LieReviewer => ExecutionRequirement.Required;
}

public sealed record L3Workflow(
    bool NeedsProveIt,
    bool NeedsLaziness,
    bool NeedsLieCatcher) : WorkflowLevel
{
    public override ExecutionLevel Level => ExecutionLevel.L3;

    public override ExecutionRequirement GroundStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement DesignStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement ContractStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement RuleStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement ArchitectureStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement TestStage => ExecutionRequirement.Absent;
    // Coordinator acts directly; edits limited to small changes as directed by human.
    public override ExecutionRequirement ImplementStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReadinessStage => ExecutionRequirement.Absent;
    public override ExecutionRequirement RefuteStage => ExecutionRequirement.Required;
    public override ExecutionRequirement GateStage => ExecutionRequirement.Required;
    public override ExecutionRequirement ReportStage => ExecutionRequirement.Required;

    public override ExecutionRequirement ProveItReviewer => NeedsProveIt
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement SolidReviewer => ExecutionRequirement.Absent;
    public override ExecutionRequirement DryReviewer => ExecutionRequirement.Required;
    public override ExecutionRequirement LazinessReviewer => NeedsLaziness
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
    public override ExecutionRequirement LieReviewer => NeedsLieCatcher
        ? ExecutionRequirement.Required
        : ExecutionRequirement.Excluded;
}
