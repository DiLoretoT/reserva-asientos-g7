# Prompts de IA - Gianluca Delmastro

Aclaro que no utilizo agentes de IA ni herramientas de programación automática. Toda la ayuda que recibo es mediante consultas en el chat de ChatGPT, en su versión gratuita.

## Desarrollo de Shared.Observability

**29/10/2026 – Organización de la solución**

- Estoy trabajando en la parte de observabilidad del TP y quiero entender cómo organizar los componentes dentro de Shared.Observability. Ayudame a pensar qué clases necesito y qué función tendría cada una

**30/10/2026 – Contexto de trazabilidad**

- Ya estuve avanzando con el contexto del TraceId. Revisa cómo lo estoy planteando y decirme si tiene sentido la forma en que estoy manteniendo el identificador durante el procesamiento

**30/10/2026 – Middleware HTTP**

- Estuve trabajando en el middleware para manejar el TraceId en las solicitudes HTTP. Revisa la implementación y explicarme si el flujo que armé es correcto por favor

**30/10/2026 – Interceptor gRPC del cliente**

- Necesito que Booking pueda enviar el TraceId a Payment por gRPC. Ayudame a entender cómo funcionan los interceptores del cliente y cómo llevarlo a mi implementación

**31/10/2026 – Interceptor gRPC del servidor**

- Sigamos con el lado de Payment. Quiero entender cómo recibir el TraceId que manda Booking y cómo guardarlo en el contexto del servicio

## Serilog y Seq

**31/10/2026 – Serilog y Seq**

- Estoy avanzando con la configuración de los logs. Tengo algunas dudas sobre cómo integrar Serilog y Seq. Ayudame a revisar la configuración y entender cómo se relacionan estos componentes

**03/11/2026 – Incorporar Seq a Docker Compose**

- Estoy preparando el entorno de Docker para visualizar los logs de los servicios en Seq, revisa si esta bien la integracion en el compose.yaml 

**03/11/2026 – Verificar la configuración de Docker Compose**

- Ya agregué Seq al archivo de Docker Compose. Cómo se que la configuración esté bien y que el contenedor se esta levantando correctamente??? Revisemos también los puertos  

**04/11/2026 – Logs de Payment que no llegan a Seq**

- Un compañero me comentó que los logs de Payment no estaban llegando a Seq. Ayudame a revisar la configuración de Serilog y comparar cómo está configurado con Booking para encontrar qué puede estar faltando

**04/11/2026 – Propiedad TraceId en Seq**

- Estoy revisando los eventos en Seq, pero no encuentro la propiedad TraceId. Cómo se si el TraceId se está incorporando correctamente???

**04/11/2026 – Identificar los logs de cada servicio**

- Quiero poder distinguir fácilmente los eventos de Booking y Payment dentro de Seq. Veamos si la propiedad servicio esta enviando los datos bien, por que en algunos me aparecen y en otros bien. De paso vemos que la informacion sea correcta. 

## Problemas del entorno y Docker

**04/11/2026 – Error de conexión con Docker**

- Intenté ejecutar Docker Compose y me aparece este error

```text
failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine; check if the path is correct and if the daemon is running: open //./pipe/dockerDesktopLinuxEngine: El sistema no puede encontrar el archivo especificado.
```

Que quiere decir excatamente y como lo soluciono

**04/11/2026 – Conflicto con el contenedor de Seq**

- Al intentar levantar Seq me aparece este error

```text
Error response from daemon: Conflict. The container name "/reserva-seq" is already in use by container "caf15842d3e2417e10c13c7db4103d03b9c8e577ad803f70b0d8ce9448954d6e". You have to remove (or rename) that container to be able to reuse that name.
```

Como lo resuelvo

**04/11/2026 – Error de conexión con PostgreSQL**

- Al ejecutar Booking me aparece este error:

```text
Failed to connect to 127.0.0.1:5433
No se puede establecer una conexión ya que el equipo de destino denegó expresamente dicha conexión.
```

Necesito conectarlo sin errores para hacer una spruebas como soluciono esto

## Integración y pruebas

**04/11/2026 – Integración con Booking y Payment**

- Ya tengo los componentes de observabilidad y estoy empezando a conectarlos con Booking y Payment. Revisemos los cambios que hice para ver si la integración está bien planteada y si me falta algo

**04/11/2026 – Pruebas en Seq**

- Quiero comprobar si los logs de Booking y Payment están llegando a Seq. Guiame con las pruebas y te voy pasando lo que aparece

**04/11/2026 – Pruebas de TraceId**

- Necesito verificar si el TraceId se está propagando correctamente entre los servicios. Revisemos los resultados y veamos qué funciona y qué falta probar

**04/11/2026 – Correcciones de integración**

- Encontré algunos problemas durante las pruebas. Ayudame a revisar cada uno y a entender qué cambios habría que hacer para solucionarlos

**04/11/2026 – Revisión de la integración completa**

Un compañero por whattsapp me aviso de los siguientes problemas: 
-Booking no estaba utilizando UseTraceId() 
-los logs de Payment no llegaban a Seq
-el TraceId no se estaba propagando por gRPC 
Veamos que esta fallando 