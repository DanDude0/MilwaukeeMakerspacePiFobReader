#!/bin/bash
[[ $EUID -ne 0 ]] && echo "This script must be run as root." && exit 1

heading ()
{
	echo -e "\n\n+- $1\n"
}

echo -e '\n\n\n\n####################\n#'
echo -e '#  This will upgrade MilwaukeeMakerspacePiFobReader on this system'
echo -e '#\n####################\n'
mkdir -p /opt/MmsPiFobReader

# Update the OS
heading 'Updating Operating System'
apt-get -y update
apt-get -y dist-upgrade

# Install .Net Core
heading 'Installing .Net Core'
wget https://download.visualstudio.microsoft.com/download/pr/201cbc49-c122-4653-a6c6-0680643d9a26/1951cfc077d868a31563a5a172d18d78/dotnet-sdk-2.1.500-linux-arm.tar.gz
mkdir -p /opt/dotnet 
tar zxf dotnet-sdk-2.1.500-linux-arm.tar.gz -C /opt/dotnet --checkpoint=.10
ln -s /opt/dotnet/dotnet /usr/local/bin/
rm -f dotnet-sdk-2.1.500-linux-arm.tar.gz

# Install MmsPiFobReader
heading 'Installer MmsPiFobReader'
cd /root
curl -s https://api.github.com/repos/DanDude0/MilwaukeeMakerspacePiFobReader/releases/latest | grep -P "(?<=browser_download_url\": \")https://.*zip" -o | wget -i -
unzip -o MmsPiFobReader.zip -d /opt/MmsPiFobReader
rm -f MmsPiFobReader.zip

echo 'Upgrade completed, restarting reader'
systemctl start MmsPiFobReader