# Payment

## 1. Responsabilidad

Payment es el microservicio encargado de procesar las solicitudes de pago que recibe desde Booking. Expone una API gRPC y devuelve un resultado que Booking utiliza para decidir si confirma o rechaza la reserva.
Además, la estructura informada incluye persistencia de pagos y componentes de auditoría.

## 2. Arquitectura

La estructura comunicada por el equipo es:
```
Payment/
│
├── Payment.API/
│   ├── Protos/
│   │   └── payment.proto
│   ├── Services/
│   │   └── PaymentGrpcService.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── Payment.Application/
│   ├── DTOs/
│   │   ├── ProcessPaymentRequest.cs
│   │   └── ProcessPaymentResult.cs
│   ├── Interfaces/
│   │   ├── IPaymentRepository.cs
│   │   ├── IPaymentProcessor.cs
│   │   └── IAuditLog.cs
│   └── Services/
│       └── PaymentService.cs
│
├── Payment.Infrastructure/
│   ├── Persistence/
│   │   ├── PaymentDbContext.cs
│   │   └── PaymentRepository.cs
│   ├── Processing/
│   │   └── SimulatedPaymentProcessor.cs
│   └── Audit/
│       └── AuditLogger.cs
│
└── Payment.Domain/
    └── Entities/
        ├── AuditLog.cs
        ├── MyPayment.cs
        └── PaymentStatus.cs
```

## 3. Responsabilidad de las capas

### Payment.API

Es la capa de entrada del microservicio.

- `Protos/payment.proto`: define el contrato gRPC, es decir, los servicios, métodos y mensajes que pueden intercambiar los componentes.
- `Services/PaymentGrpcService.cs`: implementa el servicio gRPC y recibe las solicitudes externas.
- `Program.cs`: configura e inicia la aplicación y registra las dependencias.
- `appsettings.json`: contiene configuración de la aplicación. Los secretos deben mantenerse fuera del repositorio.

### Payment.Application

Contiene la lógica de aplicación y las abstracciones que necesita para procesar pagos.

- `DTOs/ProcessPaymentRequest.cs`: representa los datos de entrada que necesita el caso de uso para procesar un pago.
- `DTOs/ProcessPaymentResult.cs`: representa el resultado del procesamiento.
- `Interfaces/IPaymentRepository.cs`: abstrae las operaciones de persistencia de pagos.
- `Interfaces/IPaymentProcessor.cs`: abstrae el mecanismo que procesa o simula el pago.
- `Interfaces/IAuditLog.cs`: abstrae el registro de auditoría.
- `Services/PaymentService.cs`: coordina el procesamiento del pago utilizando las interfaces necesarias.

### Payment.Infrastructure

Implementa los detalles técnicos requeridos por la capa de aplicación.

- `Persistence/PaymentDbContext.cs`: representa el contexto de persistencia.
- `Persistence/PaymentRepository.cs`: implementa las operaciones de acceso a los datos.
- `Processing/SimulatedPaymentProcessor.cs`: implementa el procesamiento simulado del pago.
- `Audit/AuditLogger.cs`: implementa el registro de auditoría.

### Payment.Domain

Contiene las entidades y conceptos propios del dominio de pagos.

- `AuditLog.cs`: representa información de auditoría.
- `MyPayment.cs`: representa un pago.
- `PaymentStatus.cs`: representa los estados posibles de un pago.

## 4. Flujo de procesamiento

1. Booking construye una solicitud de reserva.
2. Booking invoca el método gRPC definido por Payment.
3. `PaymentGrpcService` recibe la solicitud.
4. `PaymentService` coordina el procesamiento utilizando las interfaces de repositorio, procesador y auditoría.
5. `SimulatedPaymentProcessor` ejecuta el procesamiento simulado, según su implementación.
6. `PaymentRepository` persiste la información del pago cuando corresponde.
7. `AuditLogger` registra los sucesos de auditoría.
8. Payment devuelve el resultado a Booking mediante la respuesta gRPC.

## 5. Contrato gRPC

El contrato se encuentra en: Payment.API/Protos/payment.proto

Documentar a partir de ese archivo:
- Nombre del servicio gRPC: PaymentGrpc
- Nombre del método de procesamiento: Payment.Grpc
- Campos del mensaje de solicitud: booking_id // amount
- Campos del mensaje de respuesta: booking_id // amount // payment_status // reason
- Códigos o estados de resultado: 0 / 1 / 2

## 6. Persistencia

La persistencia se organiza mediante `PaymentDbContext` y `PaymentRepository`.
- Motor de base de datos utilizado: SQLite
- Nombre: PaymentDb
- Tablas: Auditlogger y Payment

## 7. Auditoría

La abstracción `IAuditLog` y su implementación `AuditLogger` permiten separar el uso del registro de auditoría de su mecanismo concreto.
Este componente se encarga de dejar un registro de cada movimineto que pasa por el microservicio de Payment, tanto cuando se recibe una solicitud hasta cuando se envia el resultado. Este resultado puede ser de distintos tipos:
1. Pago Rechazado
2. Pago Aceptado
3. Pago Duplicado
4. Error tecnico

