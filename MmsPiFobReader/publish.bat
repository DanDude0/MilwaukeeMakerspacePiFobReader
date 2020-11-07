"D:\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@192.168.86.132 systemctl stop MmsPiFobReader
del /Q /S bin\Release\publish
dotnet publish -c Release -o bin\Release\publish --self-contained -r linux-arm
"D:\PuTTY\pscp.exe" -batch -r -i "..\MmsPiFobReader Private.ppk" "bin\Release\publish\*" root@192.168.86.132:/opt/MmsPiFobReader/
"D:\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@192.168.86.132 systemctl start MmsPiFobReader