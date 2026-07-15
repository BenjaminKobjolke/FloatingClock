REM @echo off
cd D:\GIT\BenjaminKobjolke\release-tool

call uv run python -m release_tool "%~dp0..\FloatingClock\bin\Release\FloatingClock.exe" "%~dp0publish_settings.ini" --previous-version 2.0.0 --verbose

cd "%~dp0"
