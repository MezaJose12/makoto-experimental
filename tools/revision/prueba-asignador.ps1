<#
T2-06 · REV-TEC 03: demostración del Asignador de condiciones contra la API (local o Neon).

Qué hace:
  1. Lee las cuotas actuales (GET /api/admin/cuotas).
  2. Envía N solicitudes POST /api/sesiones AL MISMO TIEMPO (HttpClient + Task.WhenAll,
     funciona en Windows PowerShell 5.1 y en PowerShell 7).
  3. Vuelve a leer las cuotas y comprueba que no hubo colisiones: todas las solicitudes
     crearon sesión y las celdas quedan balanceadas (diferencia máxima de 1 si partían balanceadas).
  4. Descarga el audio de una de las sesiones y comprueba que llega como audio/mpeg.

Las sesiones se crean con consentimientoVersion = "prueba-rev-tec-03" para poder borrarlas
después con tools/revision/limpiar-pruebas.sql.

Uso (desde la raíz del repo, con la API levantada):
  powershell -ExecutionPolicy Bypass -File tools\revision\prueba-asignador.ps1 -ClaveAdmin <clave> [-Sesiones 30] [-Api http://localhost:8080] [-CodigoAcceso <código>]
#>
param(
    [string]$Api = "http://localhost:8080",
    [Parameter(Mandatory = $true)][string]$ClaveAdmin,
    [int]$Sesiones = 30,
    [string]$CodigoAcceso = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
# .NET Framework (PowerShell 5.1) abre solo 2 conexiones por servidor: sin esto no habría concurrencia real
[Net.ServicePointManager]::DefaultConnectionLimit = 200
$cabeceraAdmin = @{ "X-Admin-Key" = $ClaveAdmin }

# En Windows PowerShell 5.1, Invoke-RestMethod entrega el arreglo JSON como un solo objeto;
# recorrerlo aquí garantiza una lista de 6 celdas en ambas versiones.
function Leer-Cuotas {
    $respuesta = Invoke-RestMethod -Uri "$Api/api/admin/cuotas" -Headers $cabeceraAdmin
    foreach ($celda in $respuesta) {
        $celda | Add-Member -NotePropertyName ocupadas -NotePropertyValue ($celda.completadas + $celda.enCurso) -PassThru
    }
}

function Diferencia($cuotas) {
    (($cuotas | Measure-Object ocupadas -Maximum).Maximum) - (($cuotas | Measure-Object ocupadas -Minimum).Minimum)
}

Write-Host "API: $Api · solicitudes simultáneas: $Sesiones" -ForegroundColor Cyan
$antes = @(Leer-Cuotas)
$spreadAntes = Diferencia $antes

# ── Solicitudes simultáneas ──
$cuerpo = @{ consentimientoAceptado = $true; consentimientoVersion = "prueba-rev-tec-03" }
if ($CodigoAcceso) { $cuerpo.codigoAcceso = $CodigoAcceso }
$json = $cuerpo | ConvertTo-Json -Compress

$cliente = New-Object System.Net.Http.HttpClient
$cliente.Timeout = [TimeSpan]::FromSeconds(60)
$reloj = [Diagnostics.Stopwatch]::StartNew()
$tareas = 1..$Sesiones | ForEach-Object {
    $contenido = New-Object System.Net.Http.StringContent($json, [Text.Encoding]::UTF8, "application/json")
    $cliente.PostAsync("$Api/api/sesiones", $contenido)
}
try { [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]$tareas) } catch { }   # los fallos de red se cuentan abajo
$reloj.Stop()

# 0 = la solicitud no llegó a la API (error de red o tiempo agotado)
$codigos = $tareas | ForEach-Object { if ($_.IsFaulted) { 0 } else { [int]$_.Result.StatusCode } }
$creadas = @($codigos | Where-Object { $_ -eq 201 }).Count
$primera = $tareas | Where-Object { -not $_.IsFaulted -and [int]$_.Result.StatusCode -eq 201 } | Select-Object -First 1
Write-Host ("Respuestas 201: {0} de {1} en {2:N0} ms" -f $creadas, $Sesiones, $reloj.Elapsed.TotalMilliseconds)
$codigos | Group-Object | ForEach-Object { Write-Host ("  HTTP {0}: {1}" -f $_.Name, $_.Count) }

# ── Cuotas después ──
$despues = @(Leer-Cuotas)
$tabla = for ($i = 0; $i -lt $despues.Count; $i++) {
    [pscustomobject]@{
        Celda       = $despues[$i].codigo
        Completadas = $despues[$i].completadas
        EnCursoAntes = $antes[$i].enCurso
        EnCurso     = $despues[$i].enCurso
        Nuevas      = $despues[$i].enCurso - $antes[$i].enCurso
        Ocupadas    = $despues[$i].ocupadas
    }
}
$tabla | Format-Table -AutoSize | Out-String | Write-Host

# El Asignador elige siempre la celda menos ocupada (completadas + en curso), así que la
# diferencia final no puede superar 1, ni la diferencia que ya había antes de la prueba.
$nuevas = ($tabla | Measure-Object Nuevas -Sum).Sum
$spread = Diferencia $despues
$limite = [Math]::Max(1, $spreadAntes)

$ok = $true
if ($creadas -ne $Sesiones) { Write-Host "FALLO: no todas las solicitudes crearon sesión." -ForegroundColor Red; $ok = $false }
# Si había reservas de más de 30 min, el Asignador las expira y la suma baja: es un aviso, no un fallo
if ($nuevas -ne $creadas)  { Write-Host "AVISO: las cuotas suman $nuevas y se crearon $creadas (¿expiraron reservas antiguas? ejecute limpiar-pruebas.sql y repita)." -ForegroundColor Yellow }
if ($spread -gt $limite)   { Write-Host "FALLO: celdas desbalanceadas (diferencia $spread, máximo $limite)." -ForegroundColor Red; $ok = $false }

# ── Entrega del audio ──
if ($primera) {
    $sesion = $primera.Result.Content.ReadAsStringAsync().Result | ConvertFrom-Json
    $audio = $cliente.GetAsync("$Api$($sesion.audioUrl)").Result
    $tipo = $audio.Content.Headers.ContentType.MediaType
    $bytes = $audio.Content.ReadAsByteArrayAsync().Result.Length
    Write-Host ("Audio de la sesión {0}: HTTP {1}, {2}, {3:N0} bytes" -f $sesion.sesionId, [int]$audio.StatusCode, $tipo, $bytes)
    if ($tipo -ne "audio/mpeg") { Write-Host "FALLO: el audio no llegó como audio/mpeg." -ForegroundColor Red; $ok = $false }
}
$cliente.Dispose()

if ($ok) { Write-Host "APROBADO: sin colisiones, celdas balanceadas (diferencia $spread) y audio entregado." -ForegroundColor Green; exit 0 }
exit 1
