@echo off
title Regenerate
:: This will regenerate files for deployment (infra, and so on).

set scriptdir=%~dp0
if not [%scriptdir:~-1%]==[\] set scriptdir=%scriptdir%\
::echo %scriptdir%

if exist "%scriptdir%azure.yaml" del "%scriptdir%azure.yaml"
if exist "%scriptdir%infra" rmdir /s /q "%scriptdir%infra"
if exist "%scriptdir%next-steps.md" del "%scriptdir%next-steps.md"
if exist "%scriptdir%src\UKHO.ADDS.EFS.LocalHost\infra" rmdir /s /q "%scriptdir%src\UKHO.ADDS.EFS.LocalHost\infra"
if exist "%scriptdir%.azure" rmdir /s /q "%scriptdir%.azure"

pause

azd init
azd infra gen

title Regenerate - done
pause
exit /b 0
