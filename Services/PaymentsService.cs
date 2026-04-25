using APIlog.Server.DTOs.Payments;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class PaymentsService : IPaymentsService
{
    private readonly BookstoreDbContext _db;

    public PaymentsService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentDto> AddPaymentAsync(int salesReceiptId, AddPaymentDto dto)
    {
        var receiptExists = await _db.SalesReceipts.AnyAsync(sr => sr.SalesReceiptId == salesReceiptId);
        if (!receiptExists)
            throw new KeyNotFoundException($"SalesReceipt {salesReceiptId} not found.");

        var payment = new Payment
        {
            SalesReceiptId = salesReceiptId,
            PaymentNumber = Guid.NewGuid().ToString(),
            PaymentMethodId = dto.PaymentMethodId,
            PaymentAmount = dto.PaymentAmount
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var method = await _db.PaymentMethods.FindAsync(dto.PaymentMethodId);

        return new PaymentDto(
            payment.PaymentId,
            payment.PaymentNumber,
            payment.PaymentDateTime,
            payment.PaymentAmount,
            payment.PaymentMethodId,
            method?.PaymentMethodName
        );
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        var payment = await _db.Payments.FindAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();
    }
}
