@echo off
echo MiniDeck Clean Build and Distribution Creator

set APP_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck\bin\Release
set README_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt
set DIST_DIR=c:\Users\s_kaz\source\repos\MiniDeck\SimpleDistribution
set ZIP_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck-SimpleDistribution.zip
set VS_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community"
set SOLUTION_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck.sln

rem 以前のビルド成果物をクリーンアップ
echo Cleaning previous builds...
if exist %APP_PATH% rmdir /s /q %APP_PATH%
if exist %DIST_DIR% rmdir /s /q %DIST_DIR%
if exist %ZIP_PATH% del /f /q %ZIP_PATH%

rem リビルド
echo Rebuilding MiniDeck in Release mode...
%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe %SOLUTION_PATH% /t:MiniDeck:Clean;MiniDeck:Rebuild /p:Configuration=Release

echo Checking if application was built successfully...
if not exist %APP_PATH%\MiniDeck.exe (
    echo Failed to build MiniDeck application.
    goto :end
)

echo Creating distribution directory...
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
    
    rem ZIPファイルの作成日時を表示
    powershell -Command "Get-Item '%ZIP_PATH%' | Select-Object FullName, LastWriteTime | Format-List"
) else (
    echo Failed to create distribution package.
)

:end
pause
