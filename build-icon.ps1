$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = @()
foreach ($size in $sizes) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 512.0),($size / 512.0)))
    $black = [System.Windows.Media.Brushes]::Black
    $outline = New-Object System.Windows.Media.Pen $black,34
    $outline.StartLineCap = [System.Windows.Media.PenLineCap]::Round
    $outline.EndLineCap = [System.Windows.Media.PenLineCap]::Round
    $outline.LineJoin = [System.Windows.Media.PenLineJoin]::Round
    $drawing.DrawRoundedRectangle($null,$outline,(New-Object System.Windows.Rect 24,24,464,464),76,76)
    $drawing.DrawGeometry($null,$outline,[System.Windows.Media.Geometry]::Parse('M108 258V104H328'))
    $arrow = [System.Windows.Media.Geometry]::Parse('M310 212L344 246H322V298H374V276L408 310L374 344V322H322V374H344L310 408L276 374H298V322H246V344L212 310L246 276V298H298V246H276Z')
    $arrowPen = New-Object System.Windows.Media.Pen $black,10
    $arrowPen.LineJoin = [System.Windows.Media.PenLineJoin]::Round
    $drawing.DrawGeometry($black,$arrowPen,$arrow)
    $drawing.Pop()
    $drawing.Close()
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size,$size,96,96,([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = New-Object IO.MemoryStream
    $encoder.Save($stream)
    $frames += ,$stream.ToArray()
    $stream.Dispose()
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'assets\Reswin.ico'))
$writer = New-Object IO.BinaryWriter $output
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i=0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose(); $output.Dispose() }
