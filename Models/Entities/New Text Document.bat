@echo off
setlocal enabledelayedexpansion

REM === Target folder ===
set "folder=C:\Users\RC_Student_Lab\source\repos\MiningOps\MiningOps\Models\Entities"

REM === Output file ===
set "output=%~dp0Entities_FileListAndContents.txt"

> "%output%" echo Listing files and contents from:
>> "%output%" echo "%folder%"
>> "%output%" echo.

if exist "%folder%" (
    echo ==== Folder: %folder% ==== >> "%output%"
    for %%A in ("%folder%\*") do (
        if exist "%%~fA" (
            echo. >> "%output%"
            echo --- File: %%~fA --- >> "%output%"
            type "%%~fA" >> "%output%" 2>nul
            echo. >> "%output%"
            echo ----------------------------- >> "%output%"
        )
    )
) else (
    echo Folder not found: %folder% >> "%output%"
)

echo.
echo ✅ Done! Output saved to: "%output%"
pause
