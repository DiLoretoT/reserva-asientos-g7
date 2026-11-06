classDiagram
direction TB

class PaymentGrpcService {
    <<Grpc>>
    +ProcessPayment(request, context) Task~ProcessPaymentReply~
}
class DependencyInjection {
    <<Data>>
    +AddPaymentData(services, config) IServiceCollection
}
class PaymentService {
    <<Business>>
    -IPaymentRepository _paymentRepository
    -IPaymentProcessor _paymentProcessor
    -IAuditLogger _logger
    +ProcessPayment(request) Task~ProcessPaymentResult~
}
class IPaymentRepository {
    <<interface>>
    +SaveAsync(payment) Task
    +UpdateAsync(payment) Task
    +GetByBookingIdAsync(bookingId) Task~MyPayment~
}
class IPaymentProcessor {
    <<interface>>
    +ProcessPayment(request) Task~ProcessPaymentResult~
}
class IAuditLogger {
    <<interface>>
    +LogAsync(evento, status, bookingId, detalle) Task
}
class ProcessPaymentRequest {
    <<DTO>>
    +string BookingId
    +decimal Amount
}
class ProcessPaymentResult {
    <<DTO>>
    +string BookingId
    +decimal Amount
    +PaymentStatus PaymentStatus
    +string Reason
}
class MyPayment {
    <<entity>>
    +int Id
    +string BookingId
    +decimal Amount
    +PaymentStatus PaymentStatus
}
class AuditLog {
    <<entity>>
    +int Id
    +string Evento
    +string Estado
    +string BookingId
    +string Detalle
    +DateTime FechaUtc
}
class PaymentStatus {
    <<enumeration>>
    Pending
    Approved
    Rejected
}
class PaymentRepository {
    <<Data>>
    -PaymentDbContext _dbContext
}
class SimulatedPaymentProcess {
    <<Data>>
    -Evaluate(amount) tuple
}
class AuditLogger {
    <<Data>>
    -PaymentDbContext _dbContext
}
class PaymentDbContext {
    <<Data>>
    +DbSet~MyPayment~ Payment
    +DbSet~AuditLog~ AuditLog
}

PaymentGrpcService --> PaymentService : usa
PaymentGrpcService ..> ProcessPaymentRequest : traduce
PaymentGrpcService ..> ProcessPaymentResult : traduce
PaymentService --> IPaymentRepository
PaymentService --> IPaymentProcessor
PaymentService --> IAuditLogger
PaymentService ..> MyPayment
PaymentRepository ..|> IPaymentRepository
SimulatedPaymentProcess ..|> IPaymentProcessor
AuditLogger ..|> IAuditLogger
PaymentRepository --> PaymentDbContext
AuditLogger --> PaymentDbContext
PaymentDbContext o-- MyPayment
PaymentDbContext o-- AuditLog
MyPayment --> PaymentStatus
ProcessPaymentResult --> PaymentStatus
DependencyInjection ..> PaymentRepository : registra
DependencyInjection ..> SimulatedPaymentProcess : registra
DependencyInjection ..> AuditLogger : registra