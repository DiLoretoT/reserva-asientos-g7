# Prompts de Axel Ramos

Prompt AI Programación 4



## (búsqueda de ProcessPaymentRequest)
Estoy  haciendo la parte de Application, ya tengo armado el model.
Para el request quiero reciba los datos de la reserva y los guarde

## (búsqueda de IPaymentProcessor)
Yo tengo una interfaz que va a recibir un pago, la va a procesar y va a devolver si esta rechazada o aprobada, me harias el código

## (búsqueda del PaymentService)
Si te parece hacemos la carpeta Service de estaria el archivo que guardaria el pago

## (búsqueda del IPaymentProcessor)
Yo tengo una interfaz que va a recibir un pago, la va a procesar y va a devolver si esta rechazada o aprobada, me harias el código

## (Luego de tener problemas con las dependencias que hicieron que arranque de 0 en otra rama)
Bueno vamos a seguir, despues veo que es lo que estoy haciendo mal

## ()
El paymentRepository viene aacompañado de un PaymentDbContext no?

## (Sugerencia de VS vs código de AI)
Xq tanto en PaymentRepository como en PaymentDbContext me sugiere hacer el constructor primario y no la opcion que me da vos?

## (búsqueda del AuditLogger)
Ahora solo me falta un archivoo que me ayuda a dejar una huella de cada movimiento de reserva+pago al que llame AuditLogger

## (conexión AuditLogger con PaymentService)
La conexion con el Payment Service en que parte del archivo seria???

## /*Cambio de AI*/
## /*Para resolver el problema de dependencias decidí armar una nuevo rama y arrancar de 0 respetando los nombres y las dependencias de los proyectos ya armados, el problema vino por cambiar de nombres los proyectos e ignorar sus dependencias armadas*/

## (búsqueda de coordinar la arquitectura)
Tengo esta arquitectura para un proyecto de reserva de asientos en el que me corresponde hacer la parte de Payment, me gustaria saber como hago para acomodar mi arquitectura a los archivos que ya tengo armados. Mi arqutectura es:

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
├── Payment.Business/
│   ├── DTOs/
│   │   ├─ProcessPaymentRequest.cs
│   │   └─ProcessPaymentResult.cs
│   ├── Interfaces/
│   │   ├── IPaymentRepository.cs
│   │   ├── IPaymentProcessor.cs
|   |   └── IAuditLogger.cs
│   └── Services/
|       └── PaymentService.cs
├── Payment.Domain/
│   ├── Entities/
│   │   ├─MyPayment.cs
|   |   ├─PaymentStatus.cs
│   |   └─Auditlog.cs
├── Payment.Data/
│   ├── Persistence/
│   │   ├── PaymentDbContext.cs
│   │   └── PaymentRepository.cs
│   ├── Processing/
│   │   └SimulatedPaymentProcessor.
│   ├── Audit/
│   │   └── AuditLogger.cs
│   └── DependencyInjection.cs

## (Charla sobre el AuditLogger)
*Como harias el AuditLogger xq me parece que mejor idea crear una interface en Businees y depues crear un Audit que use la interfaz para dejar un registro de como fue la recepcion del booking y el procesamiento del pago.-
Que opinas??

*Siento que lo estas haciendo muy difícil

## (búsqueda del DependencyInjection)
Como seria el DependencyInjection.cs??

## (correxion de error y búsqueda del PaymentDb)
Me esta generando un error el AddPaymentData y me gustaria conectarlo a una base de datos asi guardo todo ahi

## (charla )
Me podrias explicar xq tendria que hacer otras 2 entidades??

Perfecto pero para que creo una entidad que modele el pago si ya la tengo. Estuve usando esa todo el proyecto. Para que crear una nueva?

## (Busqueda de mejora del PaymentRepository)
Estos son mis PaymentRepository, vos crees que le tengo que agregar algo? Porque?  +codigo

## (charla sobre la implementacion previa que no queria agregar)
Mi servicio entinedo que lo que hace es procesa un pago, lo guarda, lo actualiaza con su nuevo estado y despues lo devuelve con su estado actualizado: + codigo

## (desacuerdo con la IA)
No estoy de acuerdo en que en el var payment el PaymentStatus=result.PaymentStatus
Yo en ese payment estoy procesando el pago, no estoy actualizando el resultado, tengo que poner el estado original del pago

## (configuración NuGet Package)
Vamos un poco mas atras, quiero crear la base de datos donde voy a guardar todo, De todo lo que me pasaste tengo hecho hasta el AuditLogger  pero no tengo los NuGet Package

## (charla con IA + Payment.Grpc)
Puede ser qu elo que este molestando al programa sea que no tengo configurado el tema de la api ni lo de Grpc

## (Preguntas sobre nuevas implementaciones despues de consultar arquitecturas por otro lado)
Te puedo hacer 2 consultas?:

1.En Payment.Grpc tendria que agregar una carpeta para security y otra para middleware (no se es)
2.Tendria que agregar otro proyecto para Test?? (no se como se hacen los test ni si son necesarios)

## (preguntas sobre obligatoriedad de las posibles implemtaciones)
Estas son las consignas del tp que estamos haciendo, epero que ayude a responder las pregunts qu e me hiciste + pdf

## (trabajo para moldear el AuditLogger)
Necesito que me transformes esto:

paymentResult.PaymentStatus == PaymentStatus.Approved ? "PagoAprobado" : "PagoRechazado"

Es un funcion que devuelva un status de fomato PaymentStatus
