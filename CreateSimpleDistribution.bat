@echo off
echo MiniDeck Simple Distribution Package Creator

set APP_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck\bin\Release
set README_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt
set DIST_DIR=c:\Users\s_kaz\source\repos\MiniDeck\SimpleDistribution
set ZIP_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck-SimpleDistribution.zip

echo Building MiniDeck in Release mode...
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck.sln /p:Configuration=Release /t:MiniDeck /m

echo Checking if application was built successfully...
if not exist %APP_PATH%\MiniDeck.exe (
    echo Failed to build MiniDeck application.
    goto :end
)

echo Creating distribution directory...
if exist %DIST_DIR% rmdir /s /q %DIST_DIR%
mkdir %DIST_DIR%

echo Copying application files to distribution directory...
xcopy /e /i /y %APP_PATH%\*.* %DIST_DIR%
copy %README_PATH% %DIST_DIR%\README.txt /y

echo Creating ZIP archive...
powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%DIST_DIR%', '%ZIP_PATH%')"

echo.
if exist %ZIP_PATH% (
    echo Simple distribution package created successfully:
    echo %ZIP_PATH%
    echo.
    echo Note: This package does not include an installer.
    echo Users can run the application directly from the extracted folder.
) else (
    echo Failed to create distribution package.
)

:end
pause
