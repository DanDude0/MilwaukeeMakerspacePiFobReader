"D:\PuTTY\plink.exe" -i "..\MmsPiFobReader Private.ppk" root@192.168.86.149 systemctl stop MmsPiFobReader
del /Q /S bin\Release\publish
dotnet publish -c Release -o bin\Release\publish
"D:\PuTTY\pscp.exe" -batch -r -i "..\MmsPiFobReader Private.ppk" "bin\Release\publish\*" root@192.168.86.149:/opt/MmsPiFobReader/
"D:\PuTTY\plink.exe" -i "..\MmsPiFobReader Private.ppk" root@192.168.86.149 systemctl start MmsPiFobReader