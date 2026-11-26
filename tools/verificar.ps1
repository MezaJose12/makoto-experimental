<#
Verificación local de Makoto: reemplaza a GitHub Actions mientras esté deshabilitado
(cuenta universitaria). Ejecútela ANTES de cada push y antes de desplegar en Render.

Hace lo mismo que .github/workflows/ci.yml:
  1. Secretos: ningún .env ni URI/contraseña de Neon en los archivos versionados.
  2. Back-end: dotnet test.
  3. PWA: npm ci, lint y build.
  4. Pipeline de audio: pytest (si existe tools/audio-pipeline/.venv).
  5. Docker Compose desde cero, en un proyecto aparte (no toca sus datos locales):
     /health, /health/ready, 6 condiciones, 60 sesiones simultáneas → 10 por celda, audio MP3.
  6. La imagen de Render (Dockerfile.render) compila.

Uso, desde la raíz del repositorio:
  powershell -ExecutionPolicy Bypass -File tools\verificar.ps1            # todo
  powershell -ExecutionPolicy Bypass -File tools\verificar.ps1 -SinDocker # solo 1 a 4 (rápido)
#>
param([switch]$SinDocker)

$ErrorActionPreference = "Continue"
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz
$resultados = New-Object System.Collections.ArrayList

function Registrar($paso, $ok, $detalle = "") {
    [void]$resultados.Add([pscustomobject]@{ Paso = $paso; Resultado = $(if ($ok) { "OK" } else { "FALLO" }); Detalle = $detalle })
    $color = if ($ok) { "Green" } else { "Red" }
    Write-Host ("[{0}] {1} {2}" -f $(if ($ok) { "OK" } else { "FALLO" }), $paso, $detalle) -ForegroundColor $color
}

# ── 1. Secretos ──────────────────────────────────────────────────────────
Write-Host "`n== 1. Secretos en los archivos versionados ==" -ForegroundColor Cyan
$versionados = git ls-files
$envVersionados = $versionados | Where-Object { $_ -match '(^|/)\.env$' -or ($_ -match '(^|/)\.env\.' -and $_ -notmatch '\.env\.(example|development|production)$') }
# URI con contraseña real (los ejemplos del README usan usuario:clave) y contraseñas de Neon (npg_…)
$patron = 'npg_[A-Za-z0-9]{8,}|postgres(ql)?://(?!usuario:clave@)[^:/\s]+:[^@\s]{6,}@'
$hallazgos = $versionados | Where-Object { Test-Path $_ -PathType Leaf } |
    Where-Object { $_ -notmatch '\.(mp3|wav|png|jpg|xlsx|drawio)$' } |
    ForEach-Object { Select-String -Path $_ -Pattern $patron -List } |
    ForEach-Object { "$($_.Path | Resolve-Path -Relative):$($_.LineNumber)" }
if ($envVersionados) { Registrar "Archivos .env en Git" $false ($envVersionados -join ", ") } else { Registrar "Archivos .env en Git" $true }
if ($hallazgos) { Registrar "URI o contraseña en archivos" $false ($hallazgos -join ", ") } else { Registrar "URI o contraseña en archivos" $true }
# El historial no se puede limpiar sin reescribirlo: si aparece algo, la clave debe estar ya cambiada
$historial = git log -p --all | Select-String -Pattern 'npg_[A-Za-z0-9]{8,}' -List
if ($historial) { Write-Host "AVISO: el historial de Git contiene una contraseña de Neon (npg_…). Confirme que ya la cambió en Neon." -ForegroundColor Yellow }
if ((git config core.hooksPath) -ne ".githooks") { Write-Host "AVISO: active el control de secretos antes de cada commit: git config core.hooksPath .githooks" -ForegroundColor Yellow }

# ── 2. Back-end ──────────────────────────────────────────────────────────
Write-Host "`n== 2. Back-end: dotnet test ==" -ForegroundColor Cyan
dotnet test Makoto.slnx --nologo
Registrar "dotnet test" ($LASTEXITCODE -eq 0)

# ── 3. PWA ───────────────────────────────────────────────────────────────
Write-Host "`n== 3. PWA: npm ci, lint y build ==" -ForegroundColor Cyan
Push-Location frontend/makoto-pwa
npm ci --no-audit --no-fund; $okCi = $LASTEXITCODE -eq 0
npm run lint; $okLint = $LASTEXITCODE -eq 0
npm run build; $okBuild = $LASTEXITCODE -eq 0
Pop-Location
Registrar "PWA (ci, lint, build)" ($okCi -and $okLint -and $okBuild)

# ── 4. Pipeline de audio ─────────────────────────────────────────────────
Write-Host "`n== 4. Pipeline de audio: pytest ==" -ForegroundColor Cyan
$python = Join-Path $raiz "tools/audio-pipeline/.venv/Scripts/python.exe"
if (Test-Path $python) {
    Push-Location tools/audio-pipeline
    & $python -m pytest -q tests
    Registrar "pytest del pipeline de audio" ($LASTEXITCODE -eq 0)
    Pop-Location
} else {
    Write-Host "Omitido: no existe tools/audio-pipeline/.venv (ver README, sección Audios)." -ForegroundColor Yellow
}

# ── 5 y 6. Docker ────────────────────────────────────────────────────────
if (-not $SinDocker) {
    Write-Host "`n== 5. Docker Compose desde cero (proyecto makoto-verificacion) ==" -ForegroundColor Cyan
    # Usa .env.example (como la CI): sin Neon, clave de ejemplo y base vacía en un volumen aparte.
    $compose = @("compose", "-p", "makoto-verificacion", "--env-file", ".env.example")
    $ocupado = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
    if ($ocupado) {
        Registrar "Docker Compose" $false "El puerto 8080 está en uso: detenga su entorno (docker compose stop) y repita."
    } else {
        docker @compose up -d --build
        $vivo = $false
        for ($i = 0; $i -lt 60 -and -not $vivo; $i++) {
            try { $vivo = (Invoke-RestMethod http://localhost:8080/health -TimeoutSec 3) -eq "Healthy" } catch { Start-Sleep 2 }
        }
        Registrar "/health" $vivo
        if ($vivo) {
            try { $listo = Invoke-RestMethod http://localhost:8080/health/ready; Registrar "/health/ready" ($listo.estado -eq "Healthy") ($listo.componentes | ConvertTo-Json -Compress) }
            catch { Registrar "/health/ready" $false $_.Exception.Message }
            try {
                $condiciones = @(Invoke-RestMethod http://localhost:8080/api/admin/condiciones -Headers @{ "X-Admin-Key" = "cambie_esta_clave_admin" } | ForEach-Object { $_ })
                Registrar "6 condiciones sembradas" ($condiciones.Count -eq 6) "($($condiciones.Count))"
            } catch { Registrar "6 condiciones sembradas" $false $_.Exception.Message }
            # 60 sesiones simultáneas sobre una base vacía deben quedar exactamente 10 por celda
            powershell -ExecutionPolicy Bypass -File tools/revision/prueba-asignador.ps1 -ClaveAdmin cambie_esta_clave_admin -Sesiones 60
            Registrar "Concurrencia: 60 sesiones → 10 por celda y audio MP3" ($LASTEXITCODE -eq 0)
        } else {
            docker @compose logs backend
        }
        docker @compose down -v
    }

    Write-Host "`n== 6. Imagen de Render (Dockerfile.render) ==" -ForegroundColor Cyan
    docker build -f Dockerfile.render -t makoto-render .
    Registrar "docker build Dockerfile.render" ($LASTEXITCODE -eq 0)
}

# ── Resumen ──────────────────────────────────────────────────────────────
Write-Host "`n== Resumen ==" -ForegroundColor Cyan
$resultados | Format-Table -AutoSize | Out-String | Write-Host
if ($resultados | Where-Object { $_.Resultado -eq "FALLO" }) {
    Write-Host "NO suba ni despliegue hasta corregir los fallos." -ForegroundColor Red
    exit 1
}
Write-Host "Todo en verde: puede hacer push y desplegar en Render." -ForegroundColor Green
exit 0
