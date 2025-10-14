@echo off
REM Build FloatingClock in Debug configuration and run it

echo ========================================
echo Build and Run FloatingClock (Debug)
echo ========================================
echo.

REM Call the existing build_debug.bat script
call "%~dp0build_debug.bat"

REM Check if build was successful
if errorlevel 1 (
    echo.
    echo Build failed - not starting application.
    exit /b 1
)

echo.
echo ========================================
echo Starting FloatingClock...
echo ========================================
echo.

REM Change to the output directory (where the exe and settings.ini are)
cd /d "%~dp0..\FloatingClock\bin\Debug"

REM Start the application
start FloatingClock.exe

echo Application started.
echo.

REM Return to the tools directory
cd /d "%~dp0"

exit /b 0
