namespace Payment.Application.Models;

public class Payment
{
    //Son los atributos del pago que se van a guardar en la base de datos
    private int PaymentId { get; set; }
    public string BookingId { get; set; }= string.Empty;
    //public string Subject { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    //public string PaymentMethod { get; set; }
    private DateTime PaymentDate { get; set; } = DateTime.Now;
    public PaymentStatus PaymentStatus { get; set; }
}
