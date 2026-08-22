namespace SaviaUp.Backend.Domain.Entities;

public class OrderReceipt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public int ReceiptNumber { get; set; }
    public string ReceiptType { get; set; } = "PRE_BILLING"; // PRE_BILLING | PAYMENT
    public string Title { get; set; } = "RESUMEN DE CUENTA";
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentDetailsJson { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public Guid IssuedByUserId { get; set; }
    public string IssuedByUserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public virtual Order? Order { get; set; }
}
