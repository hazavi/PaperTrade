namespace PaperTrade.Domain.Competitions;

public sealed class Competition
{
    private Competition() { }
    public Competition(Guid id, Guid ownerId, string name, string joinCode, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        Id = id; OwnerId = ownerId; Name = name; JoinCode = joinCode; StartsAt = startsAt; EndsAt = endsAt;
    }
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = "";
    public string JoinCode { get; private set; } = "";
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
}

public sealed class CompetitionMember
{
    private CompetitionMember() { }
    public CompetitionMember(Guid competitionId, Guid userId, DateTimeOffset joinedAt)
    {
        CompetitionId = competitionId; UserId = userId; JoinedAt = joinedAt;
    }
    public Guid CompetitionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public decimal? StartingEquity { get; private set; }
    public decimal? EndingEquity { get; private set; }
    public void RecordStartingEquity(decimal equity) { if (equity > 0 && StartingEquity is null) StartingEquity = equity; }
    public void RecordEndingEquity(decimal equity) { if (EndingEquity is null) EndingEquity = equity; }
}
