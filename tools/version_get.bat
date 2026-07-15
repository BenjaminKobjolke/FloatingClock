@echo off
powershell -NoProfile -Command "$v=[regex]::Match((Get-Content '%~dp0..\FloatingClock\Properties\AssemblyInfo.cs' -Raw),'AssemblyVersion..(\d+\.\d+\.\d+)').Groups[1].Value; $b=(Get-Content '%~dp0..\build_version.txt').Trim(); \"${v}_$b\""
