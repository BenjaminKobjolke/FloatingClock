@echo off
powershell -NoProfile -Command "$f='%~dp0..\build_version.txt'; $n=[int](Get-Content $f).Trim()-1; Set-Content $f $n -NoNewline; $n"
