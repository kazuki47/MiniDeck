@echo off
echo MiniDeck Distribution Package Creator

set MSI_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi
set README_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt
set DIST_DIR=c:\Users\s_kaz\source\repos\MiniDeck\Distribution
set ZIP_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck-Installer.zip

echo Checking if MSI file exists...
if not exist %MSI_PATH% (
    echo MSI file not found at %MSI_PATH%
    echo Please build the installer first using Visual Studio or BuildCompleteInstaller.bat
    goto :end
)

echo Creating distribution directory...
if not exist %DIST_DIR% mkdir %DIST_DIR%

echo Copying files to distribution directory...
copy %MSI_PATH% %DIST_DIR%\MiniDeckSetup.msi /y
copy %README_PATH% %DIST_DIR%\README.txt /y

echo Creating ZIP archive...
powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%DIST_DIR%', '%ZIP_PATH%')"

echo.
if exist %ZIP_PATH% (
    echo Distribution package created successfully:
    echo %ZIP_PATH%
) else (
    echo Failed to create distribution package.
)

:end
pause
