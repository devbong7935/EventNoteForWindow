namespace EventNote.Core.Models;

/// <summary>분류(관계)별 집계 한 줄.</summary>
public sealed record RelationSummary(string Relation, int Count, long Amount, int Tickets);

public static class EventSummary
{
    private const string Unclassified = "미분류";

    /// <summary>분류별로 인원수 / 금액합계 / 식권합계를 낸다. 금액이 큰 순으로 정렬한다.</summary>
    public static IReadOnlyList<RelationSummary> ByRelation(IEnumerable<Guest> guests)
        => guests
            .GroupBy(g => string.IsNullOrWhiteSpace(g.Relation) ? Unclassified : g.Relation.Trim())
            .Select(g => new RelationSummary(
                g.Key,
                g.Count(),
                g.Sum(x => x.Amount),
                g.Sum(x => x.MealTickets)))
            .OrderByDescending(s => s.Amount)
            .ThenBy(s => s.Relation, StringComparer.CurrentCulture)
            .ToList();
}
