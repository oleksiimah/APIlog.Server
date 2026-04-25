using APIlog.Server.DTOs.Payments;

namespace APIlog.Server.Services;

public interface IPaymentsService
{
    Task<PaymentDto> AddPaymentAsync(int salesReceiptId, AddPaymentDto dto);
    Task DeletePaymentAsync(int paymentId);
}
