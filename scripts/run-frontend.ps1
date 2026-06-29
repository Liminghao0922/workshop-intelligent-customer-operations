$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot\..\src\frontend"
python -m http.server 4173
