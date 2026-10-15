$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Drawing.dll','System.Windows.Forms.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll','System.Xaml.dll') | ForEach-Object {
    $path = Join-Path $wpf $_
    if (!(Test-Path -LiteralPath $path)) { $path = Join-Path $framework $_ }
    '/r:' + $path
}
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
$icons = Get-ChildItem -LiteralPath (Join-Path $root 'assets\icons') -Filter '*.svg' | ForEach-Object { '/resource:' + $_.FullName + ',Reswin.Icons.' + $_.Name }
$icons += '/resource:' + (Join-Path $root 'assets\Reswin.ico') + ',Reswin.AppIcon'
& $csc /nologo /target:winexe /platform:x64 /optimize+ /utf8output ('/win32manifest:' + (Join-Path $root 'Reswin.manifest')) ('/win32icon:' + (Join-Path $root 'assets\Reswin.ico')) ('/out:' + (Join-Path $output 'Reswin.exe')) @references @icons @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output ('Built ' + (Join-Path $output 'Reswin.exe'))
