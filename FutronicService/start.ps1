# Script de Inicio Rápido - Futronic API Service
# Este script configura y ejecuta el servicio por primera vez

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   FUTRONIC API SERVICE - INICIO RÁPIDO" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# 1. Verificar .NET Framework
Write-Host "[1] Verificando .NET Framework 4.8..." -ForegroundColor Yellow
try {
    $dotnetVersion = Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release
    if ($dotnetVersion -ge 528040) {
     Write-Host "    ? .NET Framework 4.8 instalado" -ForegroundColor Green
    } else {
        Write-Host "    ? Se requiere .NET Framework 4.8 o superior" -ForegroundColor Red
        Write-Host " Descargar de: https://dotnet.microsoft.com/download/dotnet-framework/net48" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "    ?? No se pudo verificar .NET Framework" -ForegroundColor Yellow
}
Write-Host ""

# 2. Crear directorios necesarios
Write-Host "[2] Creando directorios..." -ForegroundColor Yellow
$directories = @(
    "C:\temp\fingerprints",
    "C:\SistemaHuellas\huellas"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
   New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "    ? Creado: $dir" -ForegroundColor Green
    } else {
        Write-Host "    ?? Ya existe: $dir" -ForegroundColor Gray
    }
}
Write-Host ""

# 3. Verificar puerto disponible
Write-Host "[3] Verificando puerto 5000..." -ForegroundColor Yellow
$portInUse = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($portInUse) {
    Write-Host "    ?? Puerto 5000 está en uso" -ForegroundColor Yellow
 Write-Host "    Proceso: $($portInUse.OwningProcess)" -ForegroundColor Gray
    
    $continue = Read-Host "    ¿Desea continuar de todos modos? (s/n)"
    if ($continue -ne "s") {
        exit 1
    }
} else {
    Write-Host "    ? Puerto 5000 disponible" -ForegroundColor Green
}
Write-Host ""

# 4. Verificar archivos del proyecto
Write-Host "[4] Verificando archivos del proyecto..." -ForegroundColor Yellow
$projectPath = Join-Path $PSScriptRoot "FutronicService.csproj"
if (Test-Path $projectPath) {
    Write-Host " ? Proyecto encontrado: $projectPath" -ForegroundColor Green
} else {
    Write-Host "    ? No se encuentra FutronicService.csproj" -ForegroundColor Red
    Write-Host "    Ejecute este script desde el directorio FutronicService/" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# 5. Compilar proyecto
Write-Host "[5] Compilando proyecto..." -ForegroundColor Yellow
Write-Host "    Esto puede tardar unos momentos..." -ForegroundColor Gray
try {
    $buildOutput = dotnet build "$projectPath" --configuration Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    ? Compilación exitosa" -ForegroundColor Green
    } else {
        Write-Host "    ? Error en compilación:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "    ?? dotnet CLI no disponible, intentando con MSBuild..." -ForegroundColor Yellow
    
    # Buscar MSBuild
    $msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
 -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
        -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($msbuildPath) {
        & $msbuildPath "$projectPath" /p:Configuration=Release /v:minimal
 if ($LASTEXITCODE -eq 0) {
       Write-Host "    ? Compilación exitosa con MSBuild" -ForegroundColor Green
        } else {
            Write-Host "    ? Error en compilación" -ForegroundColor Red
          exit 1
        }
    } else {
 Write-Host "    ? No se encuentra MSBuild" -ForegroundColor Red
        Write-Host " Compile el proyecto desde Visual Studio" -ForegroundColor Yellow
      exit 1
    }
}
Write-Host ""

# 6. Configurar firewall
Write-Host "[6] Configurando firewall..." -ForegroundColor Yellow
try {
    $firewallRule = Get-NetFirewallRule -DisplayName "Futronic API" -ErrorAction SilentlyContinue
    if (!$firewallRule) {
        $createRule = Read-Host "    ¿Crear regla de firewall para puerto 5000? (s/n)"
  if ($createRule -eq "s") {
        New-NetFirewallRule -DisplayName "Futronic API" -Direction Inbound `
       -Protocol TCP -LocalPort 5000 -Action Allow | Out-Null
            Write-Host "    ? Regla de firewall creada" -ForegroundColor Green
        } else {
Write-Host "    ?? Regla de firewall no creada" -ForegroundColor Yellow
        }
    } else {
        Write-Host "    ?? Regla de firewall ya existe" -ForegroundColor Gray
 }
} catch {
    Write-Host "    ?? No se pudo configurar firewall (requiere permisos de administrador)" -ForegroundColor Yellow
}
Write-Host ""

# 7. Mostrar configuración
Write-Host "[7] Configuración actual:" -ForegroundColor Yellow
$appsettingsPath = Join-Path $PSScriptRoot "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $config = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    Write-Host "    Puerto: $($config.Kestrel.Endpoints.Http.Url)" -ForegroundColor Gray
        Write-Host "    Threshold: $($config.Fingerprint.Threshold)" -ForegroundColor Gray
        Write-Host "  Timeout: $($config.Fingerprint.Timeout) ms" -ForegroundColor Gray
        Write-Host "    Temp Path: $($config.Fingerprint.TempPath)" -ForegroundColor Gray
        Write-Host "    CORS Origins: $($config.Cors.AllowedOrigins -join ', ')" -ForegroundColor Gray
    } catch {
        Write-Host "    ?? No se pudo leer configuración" -ForegroundColor Yellow
    }
} else {
    Write-Host "    ?? appsettings.json no encontrado" -ForegroundColor Yellow
}
Write-Host ""

# 8. Verificar dispositivo Futronic
Write-Host "[8] Verificando dispositivo Futronic..." -ForegroundColor Yellow
Write-Host "    ?? Asegúrese de que el dispositivo esté conectado" -ForegroundColor Gray
Write-Host "    ?? El servicio continuará aunque no esté conectado" -ForegroundColor Gray
Write-Host ""

# 9. Opciones de ejecución
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   OPCIONES DE EJECUCIÓN" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecutar servicio en esta ventana (Ctrl+C para detener)" -ForegroundColor White
Write-Host "2. Abrir en Visual Studio" -ForegroundColor White
Write-Host "3. Ver documentación" -ForegroundColor White
Write-Host "4. Ejecutar pruebas" -ForegroundColor White
Write-Host "5. Salir" -ForegroundColor White
Write-Host ""

$option = Read-Host "Seleccione una opción (1-5)"

switch ($option) {
    "1" {
        Write-Host ""
      Write-Host "============================================" -ForegroundColor Cyan
        Write-Host "   INICIANDO SERVICIO" -ForegroundColor Cyan
      Write-Host "============================================" -ForegroundColor Cyan
        Write-Host ""
     Write-Host "Presione Ctrl+C para detener el servicio" -ForegroundColor Yellow
Write-Host ""
      
        $exePath = Join-Path $PSScriptRoot "bin\Release\net48\FutronicService.exe"
    if (Test-Path $exePath) {
            & $exePath
    } else {
            Write-Host "? No se encuentra el ejecutable compilado" -ForegroundColor Red
   Write-Host "Ejecute primero la compilación" -ForegroundColor Yellow
    }
    }
    
    "2" {
        $slnPath = Join-Path (Split-Path $PSScriptRoot) "*.sln"
        $slnFiles = Get-ChildItem $slnPath -ErrorAction SilentlyContinue
        if ($slnFiles) {
 Write-Host "Abriendo Visual Studio..." -ForegroundColor Green
        Start-Process $slnFiles[0].FullName
     } else {
            Write-Host "? No se encuentra archivo .sln" -ForegroundColor Red
      }
    }
    
    "3" {
   $readmePath = Join-Path $PSScriptRoot "README.md"
        if (Test-Path $readmePath) {
            Write-Host "Abriendo documentación..." -ForegroundColor Green
  Start-Process $readmePath
        } else {
            Write-Host "? README.md no encontrado" -ForegroundColor Red
}
    }
    
    "4" {
    $testScriptPath = Join-Path $PSScriptRoot "test-all.ps1"
   if (Test-Path $testScriptPath) {
     Write-Host ""
  Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "   EJECUTANDO PRUEBAS" -ForegroundColor Cyan
            Write-Host "============================================" -ForegroundColor Cyan
   Write-Host ""
    Write-Host "?? Primero inicie el servicio en otra ventana" -ForegroundColor Yellow
         Write-Host ""
          $continue = Read-Host "¿El servicio está ejecutándose? (s/n)"
       if ($continue -eq "s") {
                & $testScriptPath
         }
        } else {
 Write-Host "? test-all.ps1 no encontrado" -ForegroundColor Red
        }
    }
    
    "5" {
        Write-Host "Saliendo..." -ForegroundColor Gray
        exit 0
    }
    
  default {
   Write-Host "? Opción inválida" -ForegroundColor Red
     exit 1
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   RECURSOS ÚTILES" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Documentación completa: README.md" -ForegroundColor White
Write-Host "?? Script de pruebas: test-all.ps1" -ForegroundColor White
Write-Host "?? Estado del proyecto: PROJECT_STATUS.md" -ForegroundColor White
Write-Host ""
Write-Host "URL del servicio: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Health check: http://localhost:5000/health" -ForegroundColor Cyan
Write-Host ""
