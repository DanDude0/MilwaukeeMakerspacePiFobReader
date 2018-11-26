#!/bin/bash
[[ $EUID -ne 0 ]] && echo "This script must be run as root." && exit 1

heading ()
{
	echo -e "\n\n+- $1\n"
}

echo -e '\n\n\n\n####################\n#'
echo -e '#  This will install MilwaukeeMakerspacePiFobReader on this system'
echo -e '#\n####################\n'
mkdir -p /opt/MmsPiFobReader

# Set Reader Id
while ! [[ $READERID =~ ^-?[0-9]+$ ]]
do
	heading 'Type the Reader Id you wish to set and press [ENTER]:'
	read READERID
done

echo $READERID > /opt/MmsPiFobReader/readerid.txt

# Set Server
heading '[Optional] If you wish you can hardcode the address of your API Server by typing it and pressing [ENTER]:\n+- If you wish to use SSDP just press [ENTER] without typing anything:'
read SERVER

if [[ -n "${SERVER/[ ]*\n/}" ]]
then
	echo $SERVER > /opt/MmsPiFobReader/server.txt
fi

# Work out of root user home
cd /root

# Update the OS
heading 'Updating Operating System'
apt-get -y update
apt-get -y dist-upgrade

# Install needed libraries
heading 'Installing Libraries'
apt-get -y install git libunwind8 unattended-upgrades

# Install .Net Core
heading 'Installing .Net Core'
wget https://download.visualstudio.microsoft.com/download/pr/10b96626-02d8-415a-be85-051a2a48d0c2/5ec51d3d9f092ba558fb5f1f03d26699/dotnet-sdk-2.1.500-linux-arm.tar.gz
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

# Install Systemd Unit
heading 'Installing Systemd Unit'
cat > /etc/systemd/system/MmsPiFobReader.service << 'END'
[Unit]
Description=Milwaukee Makerspace Pi Fob Reader Client
After=network.target

[Service]
WorkingDirectory=/opt/MmsPiFobReader
ExecStart=/usr/local/bin/dotnet /opt/MmsPiFobReader/MmsPiFobReader.dll
KillMode=process
Restart=always
User=root

[Install]
WantedBy=multi-user.target
END
systemctl enable MmsPiFobReader

echo 'Install completed, reboot to start reader';
