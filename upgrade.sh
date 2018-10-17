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

# Build W26 Library
heading 'Building W26 Library'
cd /root/MilwaukeeMakerspacePiFobReader
git pull
cp -rf w26reader /opt/
cd /opt/w26reader
chmod +x build.sh
./build.sh

# Build Application
heading 'Building MmsPiFobReader Application'
systemctl stop MmsPiFobReader
cd /root/MilwaukeeMakerspacePiFobReader/MmsPiFobReader
dotnet publish -c Release -o /opt/MmsPiFobReader

echo 'Upgrade completed, restarting reader'
systemctl start MmsPiFobReader