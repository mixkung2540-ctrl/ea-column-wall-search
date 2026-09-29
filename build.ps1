$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /out:"$PSScriptRoot\MakeIcon.exe" /reference:System.Drawing.dll "$PSScriptRoot\MakeIcon.cs"
if($LASTEXITCODE -ne 0){throw 'Icon build failed'}
& "$PSScriptRoot\MakeIcon.exe" $PSScriptRoot
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"$PSScriptRoot\EA_Search.ico" /out:"$PSScriptRoot\EA_Search_1.0.3.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Data.dll /reference:System.Data.DataSetExtensions.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.Xml.dll "$PSScriptRoot\EA_Search.cs" "$PSScriptRoot\Features.cs" "$PSScriptRoot\NameEditor.cs" "$PSScriptRoot\Theme.cs" "$PSScriptRoot\RoundedControls.cs" "$PSScriptRoot\Updater.cs" "$PSScriptRoot\Tests.cs"
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
