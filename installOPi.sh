#!/bin/bash
[[ $EUID -ne 0 ]] && echo "This script must be run as root." && exit 1

heading ()
{
	echo -e "\n\n+- $1\n"
}

echo -e '\n\n\n\n####################\n#'
echo -e '#  This will install MilwaukeeMakerspacePiFobReader on this system'
echo -e '#\n####################\n'
mkdir -p /opt/MmsPiW26Interface
mkdir -p /opt/MmsPiFobReader

# Set Reader Id
if [ ! -f /opt/MmsPiFobReader/readerid.txt ]
then
	while ! [[ $READERID =~ ^-?[0-9]+$ ]]
	do
		heading 'Type the Reader Id you wish to set and press [ENTER]:'
		read READERID
	done
	
	echo $READERID > /opt/MmsPiFobReader/readerid.txt
fi

# Set Server
if [ ! -f /opt/MmsPiFobReader/server.txt ]
then
	heading '[Optional] If you wish you can hardcode the address of your API Server by typing it and pressing [ENTER]:\n+- If you wish to use SSDP just press [ENTER] without typing anything:'
	read SERVER

	if [[ -n "${SERVER/[ ]*\n/}" ]]
	then
		echo $SERVER > /opt/MmsPiFobReader/server.txt
	fi
fi

# Work out of root user home
cd /root

# Update the OS
heading 'Updating Operating System'
apt-get -y update
apt-get -y dist-upgrade

# Install needed libraries
heading 'Installing Libraries'
apt-get -y install git libunwind8 busybox-syslogd ntp libgpiod-dev gpiod

# Remove unneeded libraries
heading 'Removing Libraries'
apt-get -y purge anacron unattended-upgrades logrotate dphys-swapfile rsyslog
apt-get -y autoremove

# Setup Screen Hardware
heading 'Setting Up Screen Hardware'
if [ -f "/boot/armbianEnv.txt.bak" ]
then
	cp -f /boot/armbianEnv.txt.bak /boot/armbianEnv.txt
fi
sed -i.bak 's/rootfstype=ext4/rootfstype=ext4\noverlays=spi-spidev spi-add-cs1\nparam_spidev_spi_bus=0\nparam_spidev_spi_cs=1\nextraargs=consoleblank=0 vt.global_cursor_default=0/g' /boot/armbianEnv.txt
echo -e 'fbtft\nfbtft_device' > /etc/modules-load.d/fbtft.conf
echo 'options fbtft_device rotate=270 name=piscreen speed=16000000 gpios=reset:2,dc:71 txbuflen=32768' > /etc/modprobe.d/fbtft.conf

# Install SSH Key
heading 'Installing SSH Key'
mkdir -p ~/.ssh && touch ~/.ssh/authorized_keys
chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys
echo 'ssh-rsa AAAAB3NzaC1yc2EAAAABJQAAAgEAvaHWKFt0zD0HAiv/PrTT/Qx0g4RxRAnbCrxO2C9oqPifXtcuKTW0aWNp0NyQMEQz/KFLZtzyRJi3A7rjEOH/6qotuhV7T5KJT39OgCL4A4vK1e4R+h8ZmitgIPGR5HYzW8X+1ODhGhO04nLh7LELHdYxy9gZWaMFNC3F1hlkCJF3WepkMClFoqt4pv8UHA4yWjS8eaudvcdPnoSdO2bAAuoCao/tB78dJomkxV8laDENb5wgwqNv/1p3yxTK+y1srxRcJyr6dFS6us9puVEfbpWFuMrW9v/NxylcCa1TOp1F3iKCkg8WQQPNbTC7drIh9J8Ntma/1wFPsFHfQcQ0oqgz0QDK/z8F1w1PQmh+exlK5gk0769Zt4EaTUJ8xDFf++fBIFVgGE8FnLjHRmaNfGWe3Uw0Kd9pzbXfOmcqMxvKCapTKG0c/j7fOJUIXE4BIYFB00FezEQs4e5d+rZ5Pc6SW3ue/4fB3+JoI4Kh1C2lx7SreODhgkc+uLYK31W/Z/jtJ3CwYSLz2EIdwJXLGHyPavhTxLi6mhEUpFNI85NUIOBxc2Fhx34kDEbrGPL7tiLiJo4ZfIVEog8ghgcBnxVuDFlMi/poAU8cLO2USAx0XCkF2kkyeuiBHuze/qzjO0YEXxeEapgbJnDIO0kJHONiLmjAV0DysZ1sNV8EBhs= MmsPiFobReader' > ~/.ssh/authorized_keys

# Install WiringOP
heading 'Installing WiringOP'
cd /root
rm -rfv WiringOP
git clone https://github.com/zhaolei/WiringOP.git -b h3 
cd WiringOP
chmod +x ./build
sudo ./build

# Install MmsPiW26Interface
heading 'Installing MmsPiW26Interface'
cd /opt/MmsPiW26Interface
rm -rfv *
wget https://raw.githubusercontent.com/DanDude0/MilwaukeeMakerspacePiFobReader/master/MmsPiW26Interface/MmsPiW26Interface.cpp
wget https://raw.githubusercontent.com/DanDude0/MilwaukeeMakerspacePiFobReader/master/MmsPiW26Interface/build.sh
sed -i 's/d0Line = 21;/d0Line = 199;/g' MmsPiW26Interface.cpp
sed -i 's/d0Line = 20;/d0Line = 198;/g' MmsPiW26Interface.cpp
chmod +x build.sh
./build.sh

# Install MmsPiFobReader
heading 'Installer MmsPiFobReader'
cd /tmp
mv /opt/MmsPiFobReader/readerid.txt /tmp/
mv /opt/MmsPiFobReader/server.txt /tmp/
rm -rfv /opt/MmsPiFobReader/*
curl -s https://api.github.com/repos/DanDude0/MilwaukeeMakerspacePiFobReader/releases/latest | grep -P "(?<=browser_download_url\": \")https://.*zip" -o | wget -i -
unzip -o MmsPiFobReader.zip -d /opt/MmsPiFobReader
rm -f MmsPiFobReader.zip
chmod +x /opt/MmsPiFobReader/MmsPiFobReader
mv /tmp/readerid.txt /opt/MmsPiFobReader/
mv /tmp/server.txt /opt/MmsPiFobReader/

# Install Systemd Unit
heading 'Installing Systemd Units'
cat > /etc/systemd/system/MmsPiFobReader.service << 'END'
[Unit]
Description=Milwaukee Makerspace Pi Fob Reader Client
After=network.target

[Service]
WorkingDirectory=/opt/MmsPiFobReader
ExecStart=/opt/MmsPiFobReader/MmsPiFobReader
KillMode=process
Restart=always
User=root
CPUAffinity=0 1 2

[Install]
WantedBy=multi-user.target
END
cat > /etc/systemd/system/MmsPiW26Interface.service << 'END'
[Unit]
Description=Milwaukee Makerspace Pi W26 Interface
After=network.target

[Service]
WorkingDirectory=/opt/MmsPiW26Interface
ExecStart=/opt/MmsPiW26Interface/MmsPiW26Interface
KillMode=process
Restart=always
User=root
CPUAffinity=3
Nice=-1

[Install]
WantedBy=multi-user.target
END

systemctl enable MmsPiFobReader MmsPiW26Interface

echo 'Install completed, rebooting to start reader';
reboot