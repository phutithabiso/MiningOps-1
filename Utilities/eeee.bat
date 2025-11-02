@echo off
setlocal enabledelayedexpansion

:: Output file path
set "OUTPUT_FILE=output.txt"
echo Listing files and contents from ONLY Entities and Data folders... > "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"

:: Process only these two folders
call :ProcessFolder "C:\Users\RC_Student_Lab\source\repos\MiningOps\MiningOps\Models\Entities"
call :ProcessFolder "C:\Users\RC_Student_Lab\source\repos\MiningOps\MiningOps\Data"

echo Done! Contents saved to %OUTPUT_FILE%
pause
exit /b

:: Function to process one folder
:ProcessFolder
set "FOLDER=%~1"
echo Processing folder: %FOLDER% >> "%OUTPUT_FILE%"
echo ------------------------------- >> "%OUTPUT_FILE%"

:: Loop through all files in this folder and subfolders
for /R "%FOLDER%" %%F in (*) do (
    rem Make sure the file is inside the exact folder or its subfolders
    echo File: %%F >> "%OUTPUT_FILE%"
    echo ----------- >> "%OUTPUT_FILE%"
    type "%%F" >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
)

exit /b
