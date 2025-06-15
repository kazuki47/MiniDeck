@echo off
echo MiniDeck Complete Installer Build Script

set VS_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community"
set SOLUTION_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeck.sln
set README_PATH=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt
set README_DEST=c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\README.txt

echo Building MiniDeck application in Release mode...
%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe %SOLUTION_PATH% /p:Configuration=Release /t:MiniDeck /m

echo Building installer...
%VS_PATH%\Common7\IDE\devenv.exe %SOLUTION_PATH% /Build Release /Project MiniDeckSetup

echo Checking for MSI output...
if exist c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi (
    echo MSI file successfully created at:
    echo c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi
    
    echo Copying README.txt to Release directory...
    mkdir c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release 2>nul
    copy %README_PATH% %README_DEST% /y
    
    echo Creating distribution zip file...
    powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release', 'c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckDistribution.zip')"
    
    echo Distribution package created: c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckDistribution.zip
) else (
    echo MSI file not found. Visual Studio Installer Projects may need manual intervention.
    echo Please follow these steps:
    echo 1. Open Visual Studio
    echo 2. Right-click on MiniDeckSetup project
    echo 3. Add README.txt to "File System on Target Machine" under "Application Folder"
    echo 4. Build MiniDeckSetup project
)

echo.
echo Build process completed.
echo.
echo If MSI was not created, you may need to open MiniDeckSetup.vdproj in Visual Studio 
echo and manually add README.txt to the project before building again.
pause
