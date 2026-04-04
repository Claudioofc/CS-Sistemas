@echo off
echo Iniciando CS Sistemas...

start "CS Sistemas - API" cmd /k "cd /d "%~dp0CSSistemas.API" && dotnet run"
timeout /t 3 /nobreak > nul
start "CS Sistemas - Frontend" cmd /k "cd /d "%~dp0cssistemas-web" && npm run dev"

echo.
echo API:      http://localhost:5264
echo Swagger:  http://localhost:5264/swagger
echo Frontend: http://localhost:5173
echo.
