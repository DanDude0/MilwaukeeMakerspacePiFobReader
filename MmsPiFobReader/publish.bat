"C:\Program Files\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@10.1.4.183 systemctl stop MmsPiFobReader
del /Q /S bin\Release\publish
dotnet publish -c Debug -o bin\Debug\publish --self-contained -r linux-arm
"C:\Program Files\PuTTY\pscp.exe" -batch -r -i "..\MmsPiFobReader Private.ppk" "bin\Debug\publish\*" root@10.1.4.183:/opt/MmsPiFobReader/
"C:\Program Files\PuTTY\plink.exe" -batch -i "..\MmsPiFobReader Private.ppk" root@10.1.4.183 systemctl start MmsPiFobReader