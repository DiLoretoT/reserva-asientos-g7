# Reserva de asientos

Sistema de reserva de asientos para eventos, armado con microservicios en .NET 10. Es el trabajo práctico grupal de Programación IV de la Tecnicatura Universitaria en Programación (UTN), Grupo 7.

## Estructura

```
src/
├── Availability/   sitio MVC y punto de entrada público, con Clean Architecture
├── Booking/        API REST de reservas, con N-Layer
├── Payment/        API gRPC de pagos, con N-Layer
├── Auth/           servidor de autorización
└── Shared/         código común a los servicios, como la configuración de logs
contracts/          contratos entre servicios (archivos .proto de gRPC, endpoints REST y mensajes del broker)
docs/diagramas/     diagrama de arquitectura general y diagramas de clases
prompts/            historial de prompts de cada integrante
```

`ReservaAsientos.slnx` agrupa todos los proyectos para compilarlos juntos. Cada servicio corre como un proceso independiente. Availability publica las solicitudes de reserva en un broker de mensajes (Azure Service Bus), que se las entrega a Booking. Booking llama a Payment por gRPC, y Availability consulta a Booking por REST.

## Cómo levantar el proyecto

Estas instrucciones levantan Keycloak, Seq, la base de Booking y los tres servicios en una PC con Windows. El script `start_project.ps1` ejecuta todos los pasos y comprueba los requisitos. La sección "Paso a paso sin el script" reúne los comandos para correrlos a mano.

### Requisitos

- Hay que tener instalado el SDK de .NET 10 y Docker Desktop iniciado.
- Hay que clonar el repo, que abre en la rama `dev`, y ejecutar todos los comandos desde su raíz.
- Hay que tener el connection string de Azure Service Bus del Grupo 7, que el docente entrega a cada grupo. Sin ese valor, Availability usa el publicador simulado y las solicitudes no llegan a Booking.
- Tienen que estar libres los puertos 5043, 5127 y 7113 de los servicios, 8080 de Keycloak, 8081 y 5341 de Seq, y 5433 de la base de Booking.

### Con el script

Desde la raíz del repo, en una terminal de PowerShell, ejecutar:

```powershell
powershell -ExecutionPolicy Bypass -File .\start_project.ps1
```

La opción `-ExecutionPolicy Bypass` vale solo para esta ejecución y no cambia la configuración de la PC.

El script hace lo siguiente, en este orden:

1. Comprueba los requisitos y que los puertos de los servicios estén libres.
2. Crea `.env` y `src/Auth/keycloak/.env` con valores generados. Si esos archivos ya existen, no los modifica.
3. Levanta los contenedores. La primera vez, Keycloak puede tardar varios minutos en responder, y el script lo espera hasta 10 minutos.
4. Compila la solución.
5. Carga los User Secrets. Si Booking no tiene un connection string guardado, el script lo pide por consola. Con Enter y sin valor, sigue sin broker.
6. Abre una ventana para cada servicio.

Para apagar los servicios, se cierran sus ventanas. Los contenedores se apagan con `docker compose -f src/Auth/keycloak/docker-compose.yml stop` y `docker compose stop`.

### Paso a paso sin el script

1. Copiar `.env.example` a `.env` y completar `SEQ_ADMIN_PASSWORD`.
2. Copiar `src/Auth/keycloak/.env.example` a `src/Auth/keycloak/.env` y completar `AVAILABILITY_WEB_CLIENT_SECRET` y `BOOKING_API_CLIENT_SECRET` con valores propios. Cada valor se puede generar en PowerShell con `[guid]::NewGuid().ToString("N")`. `GITHUB_CLIENT_ID` y `GITHUB_CLIENT_SECRET` solo hacen falta para iniciar sesión con GitHub, como explica [la guía de autorización](docs/autorizacion.md).
3. Levantar los contenedores:

   ```powershell
   docker compose -f src/Auth/keycloak/docker-compose.yml up -d
   docker compose up -d --wait
   ```

   Antes de seguir, hay que esperar a que `http://localhost:8080/realms/reserva-asientos` responda en el navegador. La primera vez puede tardar varios minutos.

4. Compilar la solución y crear el certificado HTTPS de desarrollo, si la PC no lo tiene:

   ```powershell
   dotnet build ReservaAsientos.slnx
   dotnet dev-certs https
   ```

5. Cargar los User Secrets con `AVAILABILITY_WEB_CLIENT_SECRET` y `BOOKING_API_CLIENT_SECRET` del `.env` de Keycloak, y con el connection string de Service Bus:

   ```powershell
   dotnet user-secrets set "Keycloak:ClientSecret" "<AVAILABILITY_WEB_CLIENT_SECRET>" --project src/Availability/Availability.Web
   dotnet user-secrets set "ServiceBus:ConnectionString" "<connection string>" --project src/Availability/Availability.Web
   dotnet user-secrets set "Keycloak:ClientSecret" "<BOOKING_API_CLIENT_SECRET>" --project src/Booking/Booking.Api
   dotnet user-secrets set "ServiceBus:ConnectionString" "<connection string>" --project src/Booking/Booking.Api
   dotnet user-secrets set "ServiceBus:Enabled" "true" --project src/Booking/Booking.Api
   dotnet user-secrets set "ServiceBus:SubscriptionName" "booking" --project src/Booking/Booking.Api
   ```

6. Abrir una terminal por servicio en la raíz del repo y ejecutar un comando en cada una:

   ```powershell
   dotnet run --no-build --project src/Payment/Payment.Grpc --launch-profile http
   dotnet run --no-build --project src/Booking/Booking.Api --launch-profile http
   dotnet run --no-build --project src/Availability/Availability.Web --launch-profile https
   ```

### Dónde queda cada servicio

| Servicio | Dirección |
|---|---|
| Sitio de Availability | `https://localhost:7113` |
| Booking | `http://localhost:5127` |
| Payment | `http://localhost:5043` |
| Keycloak | `http://localhost:8080` |
| Seq | `http://localhost:8081`, con el usuario `admin` y la contraseña que figura en `SEQ_ADMIN_PASSWORD` del `.env` de la raíz |

El usuario de prueba del sitio es `usuario1`, con la contraseña `usuario1`. Si el navegador avisa que no confía en el certificado de desarrollo, se puede continuar igual o confiar en él con `dotnet dev-certs https --trust`.

### Cómo comprobar que funciona

Para probar el recorrido exitoso, se inicia sesión, se reserva un asiento libre y se toca "Ver el estado". La solicitud queda en "Confirmada".

Para probar una falla de pagos, se cierra la ventana de Payment y se reserva otro asiento libre. Esa solicitud queda en "Rechazada", con el mensaje "El servicio de pagos no respondió y no se pudo cobrar."

Si hay más de una copia de Booking con el mismo `ServiceBus:SubscriptionName`, cada mensaje le llega a una sola de ellas. Por eso, durante estas pruebas tiene que correr una sola copia de Booking.

## Booking: desarrollo local

Con Docker Desktop iniciado, ejecutar desde la raíz `docker compose up -d --wait booking-db`.
No requiere `.env`: crea PostgreSQL con credenciales ficticias de desarrollo. Luego ejecutar
`dotnet run --project src/Booking/Booking.Api --launch-profile http`; la API aplica las migraciones
de EF Core en Development. Si la base ya se creó con el SQL anterior, consultar la guía antes de arrancar.
Ver [configuración, verificaciones manuales y alcance de Booking](docs/booking-postgresql.md).

## Cómo trabajamos con Git

### Ramas

- `dev` es la rama por defecto y la de integración. `main` contiene la última versión que el grupo probó con todos los servicios integrados.
- Cada tarea se hace en una rama propia que sale de `dev`, con un nombre del formato `tipo/descripcion`, por ejemplo `feat/listar-asientos` o `docs/tabla-componentes`.

Los tipos de rama y de commit son los mismos:

- `feat` se usa para una funcionalidad nueva.
- `fix` se usa para una corrección.
- `docs` se usa para documentación.
- `refactor` se usa para reorganizar código sin cambiar su comportamiento.
- `test` se usa para pruebas.
- `chore` se usa para configuración y tareas de mantenimiento.

### Pull requests

- Una regla de protección de GitHub bloquea el push directo a `dev` y a `main`, así que todo cambio entra por pull request.
- Cada pull request necesita la revisión favorable de otro integrante. Si el autor sube cambios después, el revisor tiene que aprobar el pull request otra vez.
- `dev` pasa a `main` por pull request cuando la integración funciona.

### Commits

Los commits siguen la convención Conventional Commits, que pide un tipo y una descripción corta en el título. 
Por ejemplo:

```
feat: agrega el listado de asientos de un evento

Explicación opcional de qué cambia y por qué.

AI-assisted
```

- El cuerpo es opcional y explica el motivo del cambio.
- La última línea indica cuánto intervino la IA. `AI-full` indica que la IA escribió el código y nadie lo modificó. `AI-assisted` indica que la IA participó y una persona revisó o ajustó el resultado. `Manual` indica que el código se escribió sin IA.

## Prompts

Cada integrante guarda en `prompts/nombre-apellido.md` el historial de instrucciones que le dio a la IA.

## Service Bus con Azure (Availability y Booking)

- **Availability:** `dotnet user-secrets set "ServiceBus:ConnectionString" "<la conexión que entrega el responsable del broker>" --project src/Availability/Availability.Web` y, si se quiere cambiar el tópico, `"ServiceBus:TopicName"` con valor `grupo7-booking-topic`. Sin la connection string, el publicador simulado atiende el sitio y todo funciona igual, salvo que no sale nada por Azure.
- **Booking:** en `src/Booking/Booking.Api`, `ServiceBus:Enabled` en `true`, `ServiceBus:ConnectionString` y `ServiceBus:SubscriptionName`. La suscripción es obligatoria cuando el consumidor está activado: cada máquina que corre Booking usa la suya (`booking-<nombre>`) y una sola, la de la demo, usa `booking`. El tópico toma `grupo7-booking-topic` por defecto.
- **Advertencia:** si se configura solo Availability, los mensajes reales van al tópico y quedan sin leer (Booking con el consumidor desactivado no los toma). Si se configura solo Booking, no llega nada, porque el publicador de Availability sigue en modo simulado. La conexión la aporta el responsable del broker y se pasa por privado.

## Secretos

El connection string del broker y cualquier otra clave no se suben al repositorio. Cada uno los guarda en su máquina, fuera de los archivos versionados.

