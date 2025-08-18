// Statement.cs
namespace CreditAnalyzer.Domain.Entities;

public class Statement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string ObjectKey { get; set; } = default!;       // MinIO object key
    public string OriginalFilename { get; set; } = default!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = default!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}