@echo off
setlocal enabledelayedexpansion

:: Output file path
set "OUTPUT_FILE=output.txt"
echo Listing files and contents from ViewModels folder... > "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"

:: Only process ViewModels
call :ProcessFolder "C:\Users\RC_Student_Lab\source\repos\MiningOps\MiningOps\ViewModels"

echo Done! Contents saved to %OUTPUT_FILE%
pause
exit /b

:: Function to process folder
:ProcessFolder
set "FOLDER=%~1"
echo Processing folder: %FOLDER% >> "%OUTPUT_FILE%"
echo ------------------------------- >> "%OUTPUT_FILE%"

:: Loop through all files in this folder and subfolders
for /r "%FOLDER%" %%F in (*) do (
    echo File: %%F >> "%OUTPUT_FILE%"
    echo ----------- >> "%OUTPUT_FILE%"
    type "%%F" >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
)

exit /b
