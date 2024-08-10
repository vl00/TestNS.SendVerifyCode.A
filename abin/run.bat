@echo off & title TestNS.SendVerifyCode
cd /d %~dp0
set cd0=%cd%
cd a
set ASPNETCORE_ENVIRONMENT=Development
"%cd0%\bin\Server2.exe" --urls https://+:50000
cd /d "%cd0%"