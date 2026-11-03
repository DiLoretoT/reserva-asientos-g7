# Prompts de Luciano Casais

Tengo que crear 3 funciones para 3 a metodo de extension. Mostrame un ejemplo y si tengo que agregar una clase nueva o modificarla una ya existente

Cree una clase llamada KeycloakOptions.cs en la carpeta Shared que lee la sección "Keycloak". ahora tengo que modificar cada appsettings.json de cada servicio con sus respectivos datos?

recibi los user-secrets para la configuracion de los token para keycloak. como los agrego en una terminal?

pude levantar el proyecto en un localhost, que pasos tengo que seguir para probar que los tokens funcionan?

configurado bien mi keycloak, haz una guia con lo que cada uno copia y pega: la referencia al proyecto, las líneas en `Program.cs`, el `[Authorize]`, la sección `"Keycloak"` del `appsettings.json` y el comando de user-secrets para pasarle a mis compañeros de los distintos microservicios (booking, availability y payment) y que agregen en sus microservicios para llamar a keycloak

Quedo bien. Solo dos cosas por que el repo se actualizo hoy:

feat/auth-booking ya está en dev (#84). Donde dice "cuando esté mergeada" (3.3, 4, 5 y checklist), poné que la referencia y la validación de Booking ya están en dev y no hay que agregarlas.
En la sección 6, paso 2, agregá que Booking se corre con dotnet run --launch-profile http.
después subila tranqui para la integracion que cuando esté activo Payment le damos una última pasada.