@echo off
setlocal enabledelayedexpansion

:: Target directory
set "targetDir=C:\Users\RC_Student_Lab\source\repos\MiningOps\MiningOps\Services"

:: Output file
set "outputFile=%~dp0FileNamesAndContents.txt"

echo Collecting file names and contents from: %targetDir%
echo. > "%outputFile%"

for /R "%targetDir%" %%F in (*) do (
    echo =============================================================== >> "%outputFile%"
    echo File: %%F >> "%outputFile%"
    echo =============================================================== >> "%outputFile%"
    type "%%F" >> "%outputFile%"
    echo. >> "%outputFile%"
)

echo.
echo Done! Results saved to "%outputFile%"
pause
