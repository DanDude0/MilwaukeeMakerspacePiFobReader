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

# Install MmsPiFobReader
heading 'Installer MmsPiFobReader'
cd /root
curl -s https://api.github.com/repos/DanDude0/MilwaukeeMakerspacePiFobReader/releases/latest | grep -P "(?<=browser_download_url\": \")https://.*zip" -o | wget -i -
unzip -o MmsPiFobReader.zip -d /opt/MmsPiFobReader
rm -f MmsPiFobReader.zip

echo 'Upgrade completed, restarting reader'
systemctl start MmsPiFobReader