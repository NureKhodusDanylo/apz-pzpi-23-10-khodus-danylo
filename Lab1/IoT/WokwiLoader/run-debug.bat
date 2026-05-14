@echo off
chcp 65001 > nul
cd /d "%~dp0WokwiLoader"
dotnet run -- --debug
