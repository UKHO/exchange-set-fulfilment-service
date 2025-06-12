@echo off
:: This will remove any generated files for Aspire.

set scriptdir=%~dp0
if not [%scriptdir:~-1%]==[\] set scriptdir=%scriptdir%\
::echo %scriptdir%

if exist "%scriptdir%azure.yaml" del "%scriptdir%azure.yaml"
if exist "%scriptdir%infra" rmdir /s /q "%scriptdir%infra"
if exist "%scriptdir%next-steps.md" del "%scriptdir%next-steps.md"
if exist "%scriptdir%src\UKHO.ADDS.EFS.LocalHost\infra" rmdir /s /q "%scriptdir%src\UKHO.ADDS.EFS.LocalHost\infra"
if exist "%scriptdir%.azure" rmdir /s /q "%scriptdir%.azure"

azd init
azd infra gen

pause
exit /b 0
