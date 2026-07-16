namespace ECommerce.Promotions.Api.Domain;

public class Promotion
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public int Percent { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public int MaxUses { get; set; }
    public int UsesCount { get; set; }
    public decimal MinAmount { get; set; }
}
