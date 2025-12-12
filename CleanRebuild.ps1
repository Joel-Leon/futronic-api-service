# Script para limpiar completamente el proyecto y reconstruir
Write-Host "=== LIMPIEZA COMPLETA DEL PROYECTO ===" -ForegroundColor Cyan

# 1. Limpiar con dotnet
Write-Host "`n[1/5] Ejecutando dotnet clean..." -ForegroundColor Yellow
cd FutronicService
dotnet clean

# 2. Eliminar directorios bin y obj
Write-Host "`n[2/5] Eliminando directorios bin y obj..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

# 3. Limpiar caché de NuGet local
Write-Host "`n[3/5] Limpiando caché de NuGet..." -ForegroundColor Yellow
dotnet nuget locals all --clear

# 4. Restaurar paquetes
Write-Host "`n[4/5] Restaurando paquetes..." -ForegroundColor Yellow
dotnet restore

# 5. Reconstruir
Write-Host "`n[5/5] Reconstruyendo proyecto..." -ForegroundColor Yellow
dotnet build --no-incremental

Write-Host "`n=== LIMPIEZA COMPLETADA ===" -ForegroundColor Green
Write-Host "El proyecto ha sido limpiado y reconstruido completamente." -ForegroundColor Green
