@echo off
cd /d "%~dp0.."
dotnet run --project src\VanderCake.Mobile
if errorlevel 1 pause
