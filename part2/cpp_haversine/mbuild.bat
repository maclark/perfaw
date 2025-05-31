@echo off
setlocal

if "%~x1" neq ".cpp" (
    echo Error: Please provide a .cpp file as the argument.
    exit /b 1
)

set "input=%~1"
set "basename=%~dpn1"
set "output=%basename%.exe"

echo Compiling %input% to %output%...
g++ -Wall -Wextra "%input%" -o "%output%"

if %errorlevel% neq 0 (
    echo Compilation failed.
) else (
    echo Compilation succeeded.
)

endlocal
