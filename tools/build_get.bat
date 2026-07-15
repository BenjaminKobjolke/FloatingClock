@echo off
powershell -NoProfile -Command "(Get-Content '%~dp0..\build_version.txt').Trim()"
