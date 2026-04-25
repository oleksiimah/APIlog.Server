namespace APIlog.Server.DTOs.Payments;

public record PaymentDto(
    int PaymentId,
    string? PaymentNumber,
    DateTime? PaymentDateTime,
    decimal PaymentAmount,
    int? PaymentMethodId,
    string? PaymentMethodName
);

public record AddPaymentDto(
    int PaymentMethodId,
    decimal PaymentAmount
);
