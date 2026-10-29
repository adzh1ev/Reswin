$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
# These templates are parsed at runtime, so csc cannot catch malformed XML.
# Validate every standalone XAML literal before producing an executable.
$xamlSettings = New-Object System.Xml.XmlReaderSettings
$xamlSettings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
$xamlSettings.XmlResolver = $null
foreach ($sourceFile in (Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs')) {
    $sourceText = [System.IO.File]::ReadAllText($sourceFile.FullName)
    foreach ($literal in [regex]::Matches($sourceText, '@"((?:[^"]|"")*)"')) {
        $markup = $literal.Groups[1].Value.Replace('""', '"')
        if (!$markup.TrimStart().StartsWith('<')) { continue }
        $xmlText = New-Object System.IO.StringReader($markup)
        $xmlReader = $null
        try {
            $xmlReader = [System.Xml.XmlReader]::Create($xmlText, $xamlSettings)
            while ($xmlReader.Read()) { }
            if ($markup -match '\{x:' -and $markup -notmatch 'xmlns:x\s*=') {
                throw 'The XAML x: namespace is not declared.'
            }
        } catch {
            throw ('Invalid XAML in ' + $sourceFile.Name + ': ' + $_.Exception.Message)
        } finally {
            if ($null -ne $xmlReader) { $xmlReader.Dispose() }
            $xmlText.Dispose()
        }
    }
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$fontRoot = Join-Path $root 'assets\fonts'
foreach ($name in @('manrope-regular.ttf','manrope-medium.ttf','manrope-semibold.ttf','OFL.txt')) {
    if (!(Test-Path -LiteralPath (Join-Path $fontRoot $name))) { throw ('Missing bundled font asset: ' + $name) }
}
# WPF pack URIs read stream-valued entries from <AssemblyName>.g.resources.
# Generate the container with the Windows CLR4 ResourceWriter, even when this
# script is invoked from PowerShell7, so the output remains Framework4.8 compatible.
$fontBuild = Join-Path $root 'artifacts\font-build'
New-Item -ItemType Directory -Path $fontBuild -Force | Out-Null
$fontPackerSource = Join-Path $fontBuild 'FontResourcePacker.cs'
$fontPacker = Join-Path $fontBuild 'FontResourcePacker.exe'
$fontResources = Join-Path $fontBuild 'Reswin.g.resources'
@'
using System;
using System.IO;
using System.Resources;
internal static class FontResourcePacker {
    static int Main(string[] args) {
        var files=Directory.GetFiles(args[0]); Array.Sort(files,StringComparer.Ordinal);
        using(var writer=new ResourceWriter(args[1])) {
            foreach(var file in files) {
                string extension=Path.GetExtension(file).ToLowerInvariant();
                if(extension!=".ttf" && extension!=".otf" && extension!=".txt") continue;
                string key="assets/fonts/"+Path.GetFileName(file).ToLowerInvariant();
                writer.AddResource(key,File.OpenRead(file),true);
            }
            writer.Generate();
        }
        return 0;
    }
}
'@ | Set-Content -LiteralPath $fontPackerSource -Encoding UTF8
& $csc /nologo /target:exe /platform:anycpu /optimize+ ('/out:' + $fontPacker) $fontPackerSource
if ($LASTEXITCODE -ne 0) { throw 'Font resource packer compilation failed.' }
& $fontPacker $fontRoot $fontResources
if ($LASTEXITCODE -ne 0) { throw 'Font resource generation failed.' }
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Drawing.dll','System.Windows.Forms.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll','System.Xaml.dll') | ForEach-Object {
    $path = Join-Path $wpf $_
    if (!(Test-Path -LiteralPath $path)) { $path = Join-Path $framework $_ }
    '/r:' + $path
}
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
$icons = Get-ChildItem -LiteralPath (Join-Path $root 'assets\icons') -Filter '*.svg' | ForEach-Object { '/resource:' + $_.FullName + ',Reswin.Icons.' + $_.Name }
$icons += '/resource:' + (Join-Path $root 'assets\Reswin.ico') + ',Reswin.AppIcon'
$icons += '/resource:' + $fontResources + ',Reswin.g.resources'
& $csc /nologo /target:winexe /platform:x64 /optimize+ /utf8output ('/win32manifest:' + (Join-Path $root 'Reswin.manifest')) ('/win32icon:' + (Join-Path $root 'assets\Reswin.ico')) ('/out:' + (Join-Path $output 'Reswin.exe')) @references @icons @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$notices = 'Reswin third-party notices' + [Environment]::NewLine + [Environment]::NewLine
$notices += 'Manrope font' + [Environment]::NewLine + (Get-Content -LiteralPath (Join-Path $fontRoot 'SOURCE.txt') -Raw)
$notices += [Environment]::NewLine + (Get-Content -LiteralPath (Join-Path $fontRoot 'OFL.txt') -Raw)
$lucideLicense = Join-Path $root 'assets\LUCIDE-LICENSE.txt'
if (Test-Path -LiteralPath $lucideLicense) { $notices += [Environment]::NewLine + 'Lucide icons' + [Environment]::NewLine + (Get-Content -LiteralPath $lucideLicense -Raw) }
$notices | Set-Content -LiteralPath (Join-Path $output 'ThirdPartyNotices.txt') -Encoding UTF8
Write-Output ('Built ' + (Join-Path $output 'Reswin.exe'))
