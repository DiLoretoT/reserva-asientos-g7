# Prompts de Dylan Maidana

Historial de prompts usados para implementar Autorización en el TP, en orden cronológico.

## 2026-10-27 21:51

Leé C:\UTN\bitacora-autorizacion.md. Soy el responsable de Keycloak. Quiero crear la rama feat/autorizacion desde dev y armar el docker-compose.yml de Keycloak en src/Auth/keycloak/, respetando el contrato de la bitácora. Explicame cada paso antes de hacerlo.

## 2026-10-28 18:51

este es un plan de objetivos diarios para tener de referencia con el proyecto. fijate si lo ves bien o si sugeris algun otro cambio o modificacion para ir avanzando. es a modo de ilustracion.

## 2026-10-28 19:09

en base al auth.server ya esta confirmado que si lo cambiamos por el keycloack esta bien. asi que confirmado eso.

## 2026-10-30 18:36

Lo de shared.auth lo va a hacer mi compañero. va por el paso 5 de lo que le pase. despues de que el testing le vaya bien va a hacer el merge

## 2026-10-30 19:58

Dale. Hacé la exportación desde el contenedor que está corriendo, copiá el archivo a una carpeta temporal y limpialo con LimpiarRealm.cs, reemplazando las claves y secretos por las variables correspondientes.
Después revisá que no quede ningún secreto ni clave privada en el JSON. Antes de tocar el archivo del repo, mostrame el diff con los cambios para que pueda revisarlo. Por ahora no reemplaces nada, primero quiero ver cómo quedó y después te confirmo.

## 2026-10-30 20:04

Prefiero hacer el export real de Keycloak para asegurarnos de que el JSON tenga los cambios tal como están configurados. Hacé todo en una carpeta temporal, sin modificar el archivo del repo. Cuando termines, limpiá el JSON y revisá que no queden claves privadas ni secretos. Después mostrame el diff para poder verlo antes de reemplazar el archivo. Si algo falla, avisame qué pasó y dejá Keycloak funcionando como estaba. No avances ni modifiques nada del repo sin consultarme.

## 2026-10-30 20:15

Reemplazá el JSON del repo con el export completo de Keycloak. Después revisá que esté todo correcto y explicame brevemente los cambios para dejarlo registrado en la bitácora.

## 2026-10-30 20:41

el profe pidio que el proveedor externo funcione de verdad. no alcanza con solo justificarlo

## 2026-10-31 14:00

Actualizá dev, creá la rama feat keycloak github y guardá Limpiarrealm.cs. Antes de hacer cualquier cambio, verificá que no haya modificaciones pendientes y avisame si encontrás algo que pueda afectar el trabajo que ya hicimos.

## 2026-10-31 14:37

Dale, agregá las dos variables al .env.example y al docker-compose como te indiqué. Después verificá con docker compose config que esté todo bien y que las variables lleguen al contenedor sin mostrar sus valores. y realiza el cambio de line en el compose "127.0.0.1:8080:8080".  Mostrame los cambios cuando termines.

## 2026-10-31 14:51

Dale, crea el proveedor de Github sin mostrar los secrets. que quede activo y aparezca el botón de GitHub en el login. No hagas todavía el export del realm.

## 2026-10-31 15:16

Dale, voy a probar el login con GitHub y te aviso cómo me fue. Después verificamos que Keycloak haya creado el usuario bien. Actualizá dev y adelantá mi rama manteniendo los cambios que tengo sin commitear. Después verificá que sigan estando los dos archivos modificados y ejecutá dotnet build para comprobar que compile todo. Si aparece algún problema, avisame antes de hacer otros cambios.

## 2026-10-31 15:33

Dale, me gusta ver por donde nos encontramos. Ahora ampliá Limpiarrealm.cs, hacé el export y limpiá el JSON. Verifica que no queden secrets, claves privadas ni datos de mi usuario, y que siga estando usuario1. mostrame el diff completo contra el JSON del repo. no reemplaces el archivo hasta que lo revise y te confirme.

## 2026-10-31 15:43

Reemplazá el JSON del repo con este export. Después verificá que el archivo haya quedado bien.

## 2026-10-31 15:53

Sisi, verificá cada resultado y si algo falla, avisame antes de seguir. no modifiques mi .env. depsues, pasame un resumen y el resultado de git status.

## 2026-10-31 16:08

bueno, hace los cuatro cambios en contracts tokens. agrega la aclaración de que Keycloak se accede en la propia PC, actualizá la descripción del login, agrega la sección del proveedor Github y agrega el usuario de GitHub a la tabla. después mostrame la diferencia para revisarlo.

## 2026-11-01 00:18

Bueno, hay que resolver el conflicto del PR #79 y corregir shared.auth con los cambios que vimos. corregi el scope al pedir tokens, el cache por audiencia, el "ClockSkew" de 30 segundos, el manejo de Clientsecret y saca Addloginkeycloak. Tambien acomoda la ubicacion de la librería en la solucion y resolve los archivos de payment segun lo acordado. después compilay proba la validacion contra Keycloak. mostrame los cambios y los resultados.

## 2026-11-01 23:17

avanza con la prueba. actualiza dev, levanta los servicios y verifica los cuatro casos de autenticacipn en Booking. despues proba la consulta de asientos desde Availability. cuando termines apaga los servicios y pasame los resultados. si algo falla avisame antes de hacer cambios.

## 2026-11-02 18:40

Revisa los logs de Shared auth para que cumplan con contractslogs.md. Agregá Operacion y Resultado a los 6 logs actuales. No registres tokens ni datos sensibles. Fijate si TraceId y Servicio ya los agrega Shared.Observability. ya consulte con el responsable del contrato y no hay problema. no hagas commits ni pushes.

## 2026-11-02 22:11

entiendo que no es obligatorio por que lo nuestro no es una API, pero armá el diagrama de clases de Shared.Auth en Mermaid y guardalo en docs/diagramas, siguiendo el formato que esta. Revisalo contra el código actual y no hagas commits ni pushes. simplemente es para chequear y posiblemente sumarlo.

## 2026-11-03 23:17

Hoy nos juntamos para la integración, pero surgió un tema con Availability que quiero revisar y también con la validación del token de Payment. Quiero probar la integración por mi cuenta usando el emulador de Service Bus, todo en local y sin modificar el repo. Los mensajes no van a llegar a Azure ni afectar las pruebas que hicimos recién. Quiero entender bien el flujo, verificar que todo compile y poder comparar los resultados con lo que se integro en la reunion.

## 2026-11-04 11:05

Revisá toda la integración y asegurate de que compile toda la solución. Probá el flujo completo desde Availability hasta Booking y Payment, incluyendo la autenticación con Keycloak y los tokens de servicio. Verificá los errores y antes corregir algo me lo comunicas. No hagas commits ni pushes. Al terminar, pasame un resumen de las pruebas realizadas, los resultados y lo que siga pendiente.

## 2026-11-04 11:43

okey, veo que cuando Keycloak está apagado, Booking no puede bajar las claves que necesita para comprobar la firma de los tokens. asi que agrega un nuevo motivo en keycloakExtensions.cs antes de la firma invalida para que el log diga "clave de firma no disponible", crea una rama desde dev y probalo.

## 2026-11-04 11:52

En la última pasada a docs/autorizacion, agregá el nuevo motivo "clave de firma no disponible" y explicá cuándo aparece.

## 2026-11-04 15:22

tengo la autorizacion del encargado de Payment para las correciones, asi que ahora implementá la validación de Keycloak en Payment (#26). Revisá cómo integrar el AddValidacionKeycloak() y con respecto a payment-test hay que borrarlo. después compilá y probá la validación con tokens válidos, vencidos y con audiencia incorrecta. mostrame el dif y los resultados.
