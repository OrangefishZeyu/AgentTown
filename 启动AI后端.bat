@echo off
chcp 65001 >nul
cd /d "%~dp0Backend"
if not exist .env copy .env.example .env >nul
where py >nul 2>nul
if %errorlevel% equ 0 (
    py -3 server.py
    goto done
)
python -c "import sys; assert sys.version_info >= (3,10)" >nul 2>nul
if %errorlevel% equ 0 (
    python server.py
    goto done
)
for /d %%D in ("%ProgramFiles%\WindowsApps\OpenAI.CodexPrimaryRuntime*") do if exist "%%D\dependencies\python\python.exe" set "TOWN_PYTHON=%%D\dependencies\python\python.exe"
if defined TOWN_PYTHON (
    "%TOWN_PYTHON%" server.py
    goto done
)
echo Please install Python 3.10 or newer, then run this launcher again.
:done
pause
