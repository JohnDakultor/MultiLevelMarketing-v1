using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Services;

public sealed record QualificationInput(
    AgentStatus AgentStatus,
    string QualificationState,
    decimal PersonalSalesAmount,
    decimal PersonalBusinessVolume,
    int ActiveDirectRecruitCount,
    bool HasActiveLeftLeg,
    bool HasActiveRightLeg
);

public sealed record QualificationRules(
    bool RequireActiveAgent,
    string? RequiredQualificationState,
    decimal MinimumPersonalSales,
    decimal MinimumPersonalBusinessVolume,
    int MinimumActiveDirectRecruits,
    bool RequireActiveLeftLeg,
    bool RequireActiveRightLeg
)
{
    public static QualificationRules None() => new(false, null, 0m, 0m, 0, false, false);
}

public sealed record QualificationFailure(string Code, string Message);

public sealed record QualificationResult(bool Passed, IReadOnlyList<QualificationFailure> Failures)
{
    public string? FailureReason =>
        Passed ? null : string.Join("; ", Failures.Select(failure => failure.Message));

    public static QualificationResult Pass() => new(true, []);

    public static QualificationResult Fail(IReadOnlyList<QualificationFailure> failures) =>
        new(false, failures);
}

public sealed class QualificationEvaluator
{
    public QualificationResult Evaluate(QualificationInput input, QualificationRules rules)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(rules);
        Validate(input, rules);

        var failures = new List<QualificationFailure>();

        if (rules.RequireActiveAgent && input.AgentStatus != AgentStatus.Active)
            failures.Add(
                new QualificationFailure(
                    "AGENT_NOT_ACTIVE",
                    "The agent must be active to qualify for binary pairing."
                )
            );

        if (
            !string.IsNullOrWhiteSpace(rules.RequiredQualificationState)
            && !string.Equals(
                input.QualificationState,
                rules.RequiredQualificationState,
                StringComparison.OrdinalIgnoreCase
            )
        )
            failures.Add(
                new QualificationFailure(
                    "QUALIFICATION_STATE_NOT_MET",
                    $"The agent must have the '{rules.RequiredQualificationState}' qualification state."
                )
            );

        if (input.PersonalSalesAmount < rules.MinimumPersonalSales)
            failures.Add(
                new QualificationFailure(
                    "MINIMUM_PERSONAL_SALES_NOT_MET",
                    $"The agent requires at least {rules.MinimumPersonalSales} in personal sales."
                )
            );

        if (input.PersonalBusinessVolume < rules.MinimumPersonalBusinessVolume)
            failures.Add(
                new QualificationFailure(
                    "MINIMUM_PERSONAL_BV_NOT_MET",
                    $"The agent requires at least {rules.MinimumPersonalBusinessVolume} in personal business volume."
                )
            );

        if (input.ActiveDirectRecruitCount < rules.MinimumActiveDirectRecruits)
            failures.Add(
                new QualificationFailure(
                    "MINIMUM_ACTIVE_DIRECTS_NOT_MET",
                    $"The agent requires at least {rules.MinimumActiveDirectRecruits} active direct recruits."
                )
            );

        if (rules.RequireActiveLeftLeg && !input.HasActiveLeftLeg)
            failures.Add(
                new QualificationFailure(
                    "ACTIVE_LEFT_LEG_REQUIRED",
                    "The agent requires an active left leg."
                )
            );

        if (rules.RequireActiveRightLeg && !input.HasActiveRightLeg)
            failures.Add(
                new QualificationFailure(
                    "ACTIVE_RIGHT_LEG_REQUIRED",
                    "The agent requires an active right leg."
                )
            );

        return failures.Count == 0
            ? QualificationResult.Pass()
            : QualificationResult.Fail(failures);
    }

    private static void Validate(QualificationInput input, QualificationRules rules)
    {
        if (
            input.PersonalSalesAmount < 0
            || input.PersonalBusinessVolume < 0
            || input.ActiveDirectRecruitCount < 0
        )
            throw new DomainInvariantException("Qualification input values cannot be negative.");

        if (
            rules.MinimumPersonalSales < 0
            || rules.MinimumPersonalBusinessVolume < 0
            || rules.MinimumActiveDirectRecruits < 0
        )
            throw new DomainInvariantException("Qualification rule thresholds cannot be negative.");
    }
}
