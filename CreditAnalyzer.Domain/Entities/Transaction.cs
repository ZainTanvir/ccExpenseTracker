using System.Text.Json;

namespace CreditAnalyzer.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid StatementId { get; set; }
    public DateOnly PostedAt { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }                  // +charge, -refund
    public string Currency { get; set; } = "USD";
    public string? ReferenceId { get; set; }
    public Guid? CategoryId { get; set; }
    public JsonDocument RawMetadata { get; set; } = JsonDocument.Parse("{}");
    public string UniquenessHash { get; set; } = default!; // for idempotency

    public Account Account { get; set; } = default!;
    public Statement Statement { get; set; } = default!;
    public Category? Category { get; set; }
}