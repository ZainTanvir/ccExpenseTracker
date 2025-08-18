namespace CreditAnalyzer.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public Guid? ParentId { get; set; }

    public Category? Parent { get; set; }
}