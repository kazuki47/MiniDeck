# このスクリプトは実験的なもので、Visual Studio Installer Projectsの.vdprojファイルを
# 直接編集してREADME.txtをインストーラーに含めようとするものです。
# 注意: vdprojファイルの直接編集は通常推奨されません。可能であればVisual Studio GUIを使用してください。

# パラメーター設定
$vdprojPath = "c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\MiniDeckSetup.vdproj"
$readmePath = "c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt"
$tempFile = "c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\MiniDeckSetup_temp.vdproj"
$backupFile = "c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\MiniDeckSetup_backup.vdproj"

# バックアップ作成
Copy-Item -Path $vdprojPath -Destination $backupFile -Force

# 一意のキーを生成（GUIDベース）
$readmeKey = [guid]::NewGuid().ToString().ToUpper().Replace("-", "")

# README.txt用のエントリを作成
$readmeEntry = @"
        "Entry"
        {
        "MsmKey" = "8:_${readmeKey}"
        "OwnerKey" = "8:_UNDEFINED"
        "MsmSig" = "8:_UNDEFINED"
        }
"@

# README.txtのファイル情報を作成
$readmeFileInfo = @"
        "$($readmeKey)"
        {
        "SourcePath" = "8:README.txt"
        "TargetName" = "8:README.txt"
        "Tag" = "8:"
        "Folder" = "8:_F8745D8946AD447E90D10E2305A7F644"
        "Condition" = "8:"
        "Transitive" = "11:FALSE"
        "Vital" = "11:TRUE"
        "ReadOnly" = "11:FALSE"
        "Hidden" = "11:FALSE"
        "System" = "11:FALSE"
        "Permanent" = "11:FALSE"
        "SharedLegacy" = "11:FALSE"
        "PackageAs" = "3:1"
        "Register" = "3:1"
        "Exclude" = "11:FALSE"
        "IsDependency" = "11:FALSE"
        "IsolateTo" = "8:"
        }
"@

try {
    # vdprojファイルの内容を取得
    $content = Get-Content $vdprojPath -Raw
    
    # Hierarchy セクションにREADME.txtのエントリを追加
    if ($content -match '"Hierarchy"\s*\{\s*("Entry"\s*\{[^}]*\}\s*)*\}') {
        $hierarchySection = $Matches[0]
        $modifiedHierarchy = $hierarchySection -replace '\}$', "$readmeEntry`n    }"
        $content = $content -replace [regex]::Escape($hierarchySection), $modifiedHierarchy
    } else {
        Write-Host "Hierarchy section not found in vdproj file."
        exit 1
    }
    
    # ファイル情報セクションにREADME.txtの情報を追加
    if ($content -match '"Files"\s*\{\s*([^}]*)\}') {
        $filesSection = $Matches[0]
        $modifiedFiles = $filesSection -replace '\}$', "$readmeFileInfo`n    }"
        $content = $content -replace [regex]::Escape($filesSection), $modifiedFiles
    } else {
        Write-Host "Files section not found in vdproj file."
        exit 1
    }
    
    # 修正した内容を一時ファイルに書き出し
    $content | Set-Content -Path $tempFile
    
    # 元のファイルを置き換え
    Move-Item -Path $tempFile -Destination $vdprojPath -Force
    
    Write-Host "Successfully added README.txt to the installer project."
    Write-Host "Please build the MiniDeckSetup project in Visual Studio to generate the MSI file."
    
} catch {
    Write-Host "Error modifying vdproj file: $_"
    
    # エラーが発生した場合、バックアップから復元
    if (Test-Path $backupFile) {
        Move-Item -Path $backupFile -Destination $vdprojPath -Force
        Write-Host "Original vdproj file restored from backup."
    }
}
