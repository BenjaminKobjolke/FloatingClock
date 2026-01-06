@echo off
echo Building FloatingClock Release...
cd /d "%~dp0.."

REM Restore NuGet packages
nuget restore FloatingClock.sln
if errorlevel 1 (
    echo NuGet restore failed
    pause
    exit /b 1
)

REM Build the solution
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" FloatingClock.sln /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild
if errorlevel 1 (
    echo Build failed
    pause
    exit /b 1
)

echo.
echo Build complete. Output: FloatingClock\bin\Release\FloatingClock.exe
pause
