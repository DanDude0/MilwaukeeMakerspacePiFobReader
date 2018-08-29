"D:\PuTTY\plink.exe" -i "V:\Data\File Vault\Dan PC.ppk" root@192.168.86.152 systemctl stop MmsPiFobReader
del /Q /S bin\Release\publish
dotnet publish -c Release -o bin\Release\publish
"D:\PuTTY\pscp.exe" -batch -r -i "V:\Data\File Vault\Dan PC.ppk" "bin\Release\publish\*" root@192.168.86.152:/opt/MmsPiFobReader/
"D:\PuTTY\plink.exe" -i "V:\Data\File Vault\Dan PC.ppk" root@192.168.86.152 systemctl start MmsPiFobReader