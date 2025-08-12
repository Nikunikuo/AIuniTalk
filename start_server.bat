@echo off
echo ========================================
echo AIuniTalk Server Launcher
echo ========================================
echo.

REM Check Python installation
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python is not installed or not in PATH
    echo Please install Python 3.11 or higher
    pause
    exit /b 1
)

echo [INFO] Python found
echo.

REM Check if virtual environment exists
if not exist "venv" (
    echo [INFO] Creating virtual environment...
    python -m venv venv
    echo [INFO] Virtual environment created
)

REM Activate virtual environment
echo [INFO] Activating virtual environment...
call venv\Scripts\activate.bat

REM Install dependencies
echo [INFO] Installing dependencies...
pip install -r server\requirements.txt >nul 2>&1
if %errorlevel% neq 0 (
    echo [WARNING] Some dependencies might not be installed
    echo [INFO] Trying to install manually...
    pip install flask flask-socketio flask-cors python-dotenv openai
)

echo.
echo [INFO] Starting server...
echo ========================================
echo.
echo Server will run at: http://localhost:5000
echo.
echo Press Ctrl+C to stop the server
echo.
echo ========================================
echo.

REM Start the server
python server\app.py

pause