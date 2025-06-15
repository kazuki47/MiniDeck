@echo off
echo MiniDeck Installer Build Script

set VS_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community"
set SOLUTION_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck.sln

echo Building MiniDeck application in Release mode...
%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe %SOLUTION_PATH% /p:Configuration=Release /t:MiniDeck /m

echo Building installer...
%VS_PATH%\Common7\IDE\devenv.exe %SOLUTION_PATH% /Build Release /Project MiniDeckSetup

echo Checking for MSI output...
if exist c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi (
    echo MSI file successfully created at:
    echo c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi
) else (
    echo MSI file not found. Please check Visual Studio for any build errors.
    echo You may need to open Visual Studio and build the installer project manually.
)

echo.
echo Build process completed.
pause
