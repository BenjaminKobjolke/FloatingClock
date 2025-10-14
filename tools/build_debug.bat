@echo off
REM Build FloatingClock in Debug configuration

echo ========================================
echo Building FloatingClock (Debug)
echo ========================================
echo.

REM Change to solution directory
cd /d "%~dp0.."

REM Find MSBuild by checking common installation paths
set "MSBUILD_PATH="

REM Check for Visual Studio 2022
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)

REM Check for Visual Studio 2019
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)

REM Check for older MSBuild
if exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
    goto :found
)

REM MSBuild not found
echo ERROR: MSBuild not found!
echo Please install Visual Studio or Visual Studio Build Tools.
exit /b 1

:found
echo Using MSBuild: %MSBUILD_PATH%
echo.

REM Restore NuGet packages
echo Restoring NuGet packages...
"%MSBUILD_PATH%" FloatingClock.sln /t:Restore /p:Configuration=Debug
if errorlevel 1 (
    echo.
    echo ERROR: NuGet restore failed!
    exit /b 1
)
echo.

REM Build the solution
echo Building solution...
"%MSBUILD_PATH%" FloatingClock.sln /p:Configuration=Debug /p:Platform="Any CPU" /verbosity:minimal
if errorlevel 1 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo Output: FloatingClock\bin\Debug\FloatingClock.exe
echo.

REM Return to the tools directory
cd /d "%~dp0"

exit /b 0
