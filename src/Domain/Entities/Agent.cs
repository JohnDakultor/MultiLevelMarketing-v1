using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Network;

public sealed class Agent : OrganizationEntity
{
    private Agent() { }

    public string UserId { get; private set; } = string.Empty;
    public string AgentCode { get; private set; } = string.Empty;
    public string ReferralCode { get; private set; } = string.Empty;
    public Guid? SponsorAgentId { get; private set; }
    public Guid? PlacementParentAgentId { get; private set; }
    public PlacementSide? PlacementSide { get; private set; }
    public PlacementSide? PreferredLeg { get; private set; }
    public AgentStatus Status { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public string QualificationState { get; private set; } = string.Empty;

    public static Agent Apply(
        Guid organizationId,
        string userId,
        string agentCode,
        string referralCode,
        DateTimeOffset joinedAt,
        Guid? sponsorAgentId = null
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
            throw new DomainInvariantException("Organization and user are required.");
        return new Agent
        {
            OrganizationId = organizationId,
            UserId = userId,
            AgentCode = agentCode.Trim().ToUpperInvariant(),
            ReferralCode = referralCode.Trim().ToUpperInvariant(),
            SponsorAgentId = sponsorAgentId,
            JoinedAt = joinedAt,
            Status = AgentStatus.Applied,
            QualificationState = "Pending",
        };
    }

    public void SubmitForApproval()
    {
        if (Status != AgentStatus.Applied)
            throw new DomainInvariantException("Only an application can be submitted.");
        Status = AgentStatus.PendingApproval;
    }

    public void Activate(DateTimeOffset activatedAt)
    {
        if (
            Status
            is not (AgentStatus.Applied or AgentStatus.PendingApproval or AgentStatus.Inactive)
        )
            throw new DomainInvariantException("Agent cannot be activated from the current state.");
        Status = AgentStatus.Active;
        ActivatedAt = activatedAt;
        AddDomainEvent(new AgentActivatedEvent(Id));
    }

    public void Place(Guid parentAgentId, PlacementSide side)
    {
        if (parentAgentId == Id)
            throw new DomainInvariantException("An agent cannot be their own placement parent.");
        if (PlacementParentAgentId is not null)
            throw new DomainInvariantException("Agent is already placed.");
        PlacementParentAgentId = parentAgentId;
        PlacementSide = side;
        AddDomainEvent(new AgentPlacedEvent(Id, parentAgentId, side));
    }

    public bool SetPreferredLeg(PlacementSide preferredLeg, DateTimeOffset occurredAt)
    {
        if (!Enum.IsDefined(preferredLeg))
            throw new DomainInvariantException("Preferred placement leg is invalid.");
        if (Status is AgentStatus.Suspended or AgentStatus.Closed)
            throw new DomainInvariantException("This Agent cannot change their preferred leg.");
        if (PreferredLeg == preferredLeg)
            return false;

        PreferredLeg = preferredLeg;
        AddDomainEvent(
            new AgentPreferredLegChangedEvent(OrganizationId, Id, preferredLeg, occurredAt)
        );
        return true;
    }

    public void MovePlacement(
        Guid newParentAgentId,
        PlacementSide newSide,
        string actorUserId,
        string reason,
        DateTimeOffset occurredAt
    )
    {
        if (PlacementParentAgentId is null || PlacementSide is null)
            throw new DomainInvariantException("Only a placed Agent can be moved.");
        if (newParentAgentId == Id)
            throw new DomainInvariantException("An Agent cannot be their own placement parent.");
        if (string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(reason))
            throw new DomainInvariantException("Placement movement requires an actor and reason.");

        var oldParent = PlacementParentAgentId.Value;
        var oldSide = PlacementSide.Value;
        if (oldParent == newParentAgentId && oldSide == newSide)
            throw new DomainInvariantException("The requested placement is unchanged.");

        PlacementParentAgentId = newParentAgentId;
        PlacementSide = newSide;
        AddDomainEvent(
            new AgentPlacementMovedEvent(
                OrganizationId,
                Id,
                oldParent,
                oldSide,
                newParentAgentId,
                newSide,
                actorUserId.Trim(),
                reason.Trim(),
                occurredAt
            )
        );
    }

    public void Suspend()
    {
        if (Status != AgentStatus.Active)
            throw new DomainInvariantException("Only an active agent can be suspended.");
        Status = AgentStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != AgentStatus.Suspended)
            throw new DomainInvariantException("Only a suspended agent can be reactivated.");
        Status = AgentStatus.Active;
    }

    public void Close() => Status = AgentStatus.Closed;

    public void RejectApplication()
    {
        if (Status is not (AgentStatus.Applied or AgentStatus.PendingApproval))
            throw new DomainInvariantException("Only a pending application can be rejected.");
        Status = AgentStatus.Closed;
    }

    public void SetQualification(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new DomainInvariantException("Qualification state is required.");
        QualificationState = state;
    }

    public void RegenerateReferralCode(string referralCode)
    {
        if (Status != AgentStatus.Active)
            throw new DomainInvariantException("Only an active agent can rotate a referral code.");
        if (string.IsNullOrWhiteSpace(referralCode))
            throw new DomainInvariantException("Referral code is required.");
        ReferralCode = referralCode.Trim().ToUpperInvariant();
    }
}
