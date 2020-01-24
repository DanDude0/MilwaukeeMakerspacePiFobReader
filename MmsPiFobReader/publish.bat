"C:\Program Files\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@10.1.2.136 systemctl stop MmsPiFobReader
del /Q /S bin\Release\publish
dotnet publish -c Release -o bin\Release\publish --self-contained -r linux-arm
"C:\Program Files\PuTTY\pscp.exe" -batch -r -i "..\MmsPiFobReader Private.ppk" "bin\Release\publish\*" root@10.1.2.136:/opt/MmsPiFobReader/
"C:\Program Files\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@10.1.2.136 systemctl start MmsPiFobReader