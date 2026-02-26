@echo off
set PROTO_DIR=proto
set CS_DIR=cs

if not exist %CS_DIR% (
    mkdir %CS_DIR%
)

for %%f in (%PROTO_DIR%\*.proto) do (
    echo Generating %%~nxf
    protogen.exe -i:%%f -o:%CS_DIR%\%%~nf.cs
)

echo Done.
pause