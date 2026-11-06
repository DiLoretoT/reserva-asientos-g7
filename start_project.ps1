# Tomás Di Loreto.
# Levanta el sistema completo en una PC con Windows. Keycloak, Seq, la base de Booking,
# Payment, Booking y Availability (issue #38). Requiere el SDK de .NET 10 y Docker Desktop.
#
# Uso, desde la raíz del repo:
#   powershell -ExecutionPolicy Bypass -File .\start_project.ps1
#
# Se puede ejecutar más de una vez, ya que no reemplaza los .env que ya existen. Los secretos quedan
# en esos .env, que git ignora, y en los User Secrets, que están fuera del repo.


[CmdletBinding()]
param(
    # Si no se pasa, se reusa el de los User Secrets o se pide por consola.
    [string]$ServiceBusConnectionString,
    # Booking, en la demo, consume de esta suscripción (contracts/broker-config.md).
    [string]$Suscripcion = 'booking'
)

# Se para en la carpeta del script, para que las rutas de abajo funcionen desde cualquier lugar.
Set-Location $PSScriptRoot

# Rutas que se usan varias veces en el resto del script.
$KeycloakCompose = 'src/Auth/keycloak/docker-compose.yml'
$KeycloakEnv = 'src/Auth/keycloak/.env'
$ProyectoAvailability = 'src/Availability/Availability.Web'
$ProyectoBooking = 'src/Booking/Booking.Api'
$ProyectoPayment = 'src/Payment/Payment.Grpc'

# Los tres servicios, cada uno con su perfil de arranque y su puerto.
# Payment va primero y Availability último, en el orden en que se abren las ventanas.
$Servicios = @(
    @{ Nombre = 'Payment'; Proyecto = $ProyectoPayment; Perfil = 'http'; Puerto = 5043 },
    @{ Nombre = 'Booking'; Proyecto = $ProyectoBooking; Perfil = 'http'; Puerto = 5127 },
    @{ Nombre = 'Availability'; Proyecto = $ProyectoAvailability; Perfil = 'https'; Puerto = 7113 }
)


# Muestra el título de cada etapa.
function Paso([string]$Texto) {
    Write-Host ''
    Write-Host "== $Texto" -ForegroundColor Cyan
}

# Muestra el error y termina el script con código 1, que significa que falló.
function Cortar([string]$Texto) {
    Write-Host ''
    Write-Host "ERROR: $Texto" -ForegroundColor Red
    exit 1
}

# Corta el script si el último comando externo terminó con error.
# $LASTEXITCODE guarda el resultado de ese comando, y 0 significa que salió bien.
function Comprobar([string]$Accion) {
    if ($LASTEXITCODE -ne 0) { Cortar "falló: $Accion" }
}

# Genera 24 bytes aleatorios y los devuelve como texto en hexadecimal.
function Nuevo-Secreto {
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

# Crea el .env a partir de su plantilla, con un valor nuevo en cada clave indicada. Si el archivo
# ya existe no lo toca, porque Keycloak y los User Secrets tienen que seguir usando los mismos valores.
function Preparar-Env([string]$Ruta, [string[]]$Claves) {
    if (Test-Path $Ruta) {
        Write-Host "$Ruta ya existe, no se modifica."
        return
    }
    # Recorre la plantilla línea por línea y reemplaza el valor de las claves indicadas.
    $lineas = Get-Content "$Ruta.example" -Encoding UTF8 | ForEach-Object {
        $linea = $_
        foreach ($clave in $Claves) {
            if ($linea -match "^$clave=") { $linea = "$clave=$(Nuevo-Secreto)" }
        }
        $linea
    }
    # Sin BOM: Docker Compose tomaría esos bytes como parte del nombre de la primera variable.
    [IO.File]::WriteAllLines((Join-Path $PSScriptRoot $Ruta), [string[]]$lineas, (New-Object Text.UTF8Encoding($false)))
    Write-Host "$Ruta creado con valores generados."
}

# Devuelve el valor de una clave de un .env, o corta el script si la clave no está.
function Leer-Env([string]$Ruta, [string]$Clave) {
    $linea = Get-Content $Ruta -Encoding UTF8 | Where-Object { $_ -match "^$Clave=" } | Select-Object -First 1
    if (-not $linea) { Cortar "$Ruta no tiene la clave $Clave." }
    return $linea.Substring($Clave.Length + 1).Trim()
}

# Intenta conectarse al puerto durante un segundo. Si alguien responde, el puerto está en uso.
function Puerto-Abierto([int]$Puerto) {
    $cliente = New-Object Net.Sockets.TcpClient
    try {
        $intento = $cliente.BeginConnect('localhost', $Puerto, $null, $null)
        return ($intento.AsyncWaitHandle.WaitOne(1000) -and $cliente.Connected)
    }
    catch { return $false }
    finally { $cliente.Close() }
}


Paso 'Comprobando los requisitos'

# Get-Command busca el programa. Si no lo encuentra, el script se corta con el motivo.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Cortar 'no se encontró dotnet. Instalá el SDK de .NET 10.' }

# Lista los SDK instalados y busca alguno cuya versión empiece con 10.
if (-not (dotnet --list-sdks | Where-Object { $_ -match '^10\.' })) { Cortar 'falta el SDK de .NET 10.' }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Cortar 'no se encontró docker. Instalá Docker Desktop.' }

# docker info necesita que Docker esté iniciado. Su salida se descarta: solo importa si falló.
$null = docker info 2>$null
if ($LASTEXITCODE -ne 0) { Cortar 'Docker no responde. Iniciá Docker Desktop y volvé a ejecutar el script.' }

# Recorre la lista $Servicios del bloque 1 y prueba el puerto de cada uno.
foreach ($servicio in $Servicios) {
    if (Puerto-Abierto $servicio.Puerto) {
        Cortar "el puerto $($servicio.Puerto) de $($servicio.Nombre) está en uso. Cerrá esa ventana y volvé a ejecutar el script."
    }
}


Paso 'Preparando los archivos .env'
# El segundo dato de cada llamada lista las claves que reciben un valor generado.
Preparar-Env '.env' @('SEQ_ADMIN_PASSWORD')
Preparar-Env $KeycloakEnv @('KEYCLOAK_ADMIN_PASSWORD', 'AVAILABILITY_WEB_CLIENT_SECRET', 'BOOKING_API_CLIENT_SECRET')

Paso 'Levantando Keycloak, Seq y la base de Booking con Docker'
# -f indica qué archivo de Compose usar. Sin -f, Docker usa el de la carpeta actual.
docker compose -f $KeycloakCompose up -d
Comprobar 'levantar Keycloak'
docker compose up -d --wait
Comprobar 'levantar Seq y la base de Booking'

Write-Host 'Esperando a que Keycloak importe el realm. La primera vez puede tardar varios minutos...'
$keycloakListo = $false
# Hasta 200 intentos. Si el pedido falla, entra al catch, espera 2 segundos y vuelve a probar.
for ($i = 0; $i -lt 200 -and -not $keycloakListo; $i++) {
    try {
        $null = Invoke-WebRequest 'http://localhost:8080/realms/reserva-asientos' -UseBasicParsing -TimeoutSec 3
        $keycloakListo = $true
    }
    catch { Start-Sleep -Seconds 2 }
}
if (-not $keycloakListo) { Cortar 'Keycloak no respondió en 10 minutos. Revisá: docker logs keycloak' }


Paso 'Compilando la solución'
dotnet build ReservaAsientos.slnx --nologo -v q
Comprobar 'compilar la solución'

Paso 'Cargando los User Secrets'
# Cada servicio recibe el client secret que le corresponde, leído del .env de Keycloak.
# Out-Null descarta el mensaje del comando para no llenar la pantalla.
dotnet user-secrets set 'Keycloak:ClientSecret' (Leer-Env $KeycloakEnv 'AVAILABILITY_WEB_CLIENT_SECRET') --project $ProyectoAvailability | Out-Null
Comprobar 'cargar el secret de Availability'
dotnet user-secrets set 'Keycloak:ClientSecret' (Leer-Env $KeycloakEnv 'BOOKING_API_CLIENT_SECRET') --project $ProyectoBooking | Out-Null
Comprobar 'cargar el secret de Booking'

# Si no vino como parámetro, se reusa el que ya esté guardado o se pide por consola.
if (-not $ServiceBusConnectionString) {
    $anterior = dotnet user-secrets list --project $ProyectoBooking | Where-Object { $_ -match '^ServiceBus:ConnectionString = ' } | Select-Object -First 1
    if ($anterior) {
        $ServiceBusConnectionString = $anterior -replace '^ServiceBus:ConnectionString = ', ''
        Write-Host 'Se reusa el connection string de Service Bus que ya estaba cargado.'
    }
    else {
        $ServiceBusConnectionString = Read-Host 'Pegá el connection string de Service Bus (Enter para seguir sin broker)'
    }
}
# Saca espacios y comillas que suelen venir al pegar.
$ServiceBusConnectionString = $ServiceBusConnectionString.Trim().Trim('"')

if ($ServiceBusConnectionString) {
    # Un connection string de Service Bus empieza así. Si no, probablemente se pegó otra cosa.
    if ($ServiceBusConnectionString -notmatch '^Endpoint=sb://') { Cortar 'el connection string tiene que empezar con Endpoint=sb://' }
    # Availability lo usa para publicar los mensajes y Booking para recibirlos.
    dotnet user-secrets set 'ServiceBus:ConnectionString' $ServiceBusConnectionString --project $ProyectoAvailability | Out-Null
    Comprobar 'cargar el connection string en Availability'
    dotnet user-secrets set 'ServiceBus:ConnectionString' $ServiceBusConnectionString --project $ProyectoBooking | Out-Null
    Comprobar 'cargar el connection string en Booking'
    dotnet user-secrets set 'ServiceBus:Enabled' 'true' --project $ProyectoBooking | Out-Null
    Comprobar 'habilitar el consumidor de Booking'
    dotnet user-secrets set 'ServiceBus:SubscriptionName' $Suscripcion --project $ProyectoBooking | Out-Null
    Comprobar 'cargar la suscripción de Booking'
}
else {
    Write-Host 'Sin connection string, Availability usa el publicador simulado y las solicitudes no llegan a Booking.' -ForegroundColor Yellow
}


Paso 'Comprobando el certificado HTTPS de desarrollo'
# --check falla si no hay certificado. En ese caso se crea uno.
dotnet dev-certs https --check | Out-Null
if ($LASTEXITCODE -ne 0) {
    dotnet dev-certs https | Out-Null
    Comprobar 'crear el certificado de desarrollo'
}

Paso 'Abriendo una ventana por servicio'
foreach ($servicio in $Servicios) {
    # --no-build porque la solución ya se compiló: varios dotnet run en paralelo pueden
    # chocar al escribir los mismos archivos de los proyectos compartidos.
    $comando = "`$host.UI.RawUI.WindowTitle = '$($servicio.Nombre)'; dotnet run --no-build --project $($servicio.Proyecto) --launch-profile $($servicio.Perfil)"
    # -NoExit deja la ventana abierta, para poder ver los logs o el error si el servicio se cae.
    Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList '-NoExit', '-Command', $comando
}

# Hasta 60 intentos por servicio, con 2 segundos de espera entre uno y otro.
foreach ($servicio in $Servicios) {
    $abierto = $false
    for ($i = 0; $i -lt 60 -and -not $abierto; $i++) {
        $abierto = Puerto-Abierto $servicio.Puerto
        if (-not $abierto) { Start-Sleep -Seconds 2 }
    }
    if (-not $abierto) { Cortar "$($servicio.Nombre) no respondió en 2 minutos. Revisá su ventana." }
    Write-Host "$($servicio.Nombre) responde en el puerto $($servicio.Puerto)."
}

Paso 'Listo'
Write-Host 'Sitio:     https://localhost:7113'
Write-Host 'Usuario:   usuario1 / usuario1'
Write-Host 'Keycloak:  http://localhost:8080'
Write-Host 'Seq:       http://localhost:8081 (admin, con la contraseña SEQ_ADMIN_PASSWORD del .env)'
Write-Host 'Para apagar los servicios, cerrá sus ventanas.'

