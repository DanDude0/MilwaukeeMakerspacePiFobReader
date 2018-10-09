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
heading 'Type the Reader Id you wish to set and press [ENTER]:'
read READERID
echo $READERID > /opt/MmsPiFobReader/readerid.txt

# Work out of root user home
cd /root

# Update the OS
heading 'Updating Operating System'
apt-get -y update
apt-get -y dist-upgrade

# Install needed libraries
heading 'Installing Libraries'
apt-get -y install git libunwind8 unattended-upgrades wiringpi

# Install .Net Core
heading 'Installing .Net Core'
wget https://download.visualstudio.microsoft.com/download/pr/10b96626-02d8-415a-be85-051a2a48d0c2/5ec51d3d9f092ba558fb5f1f03d26699/dotnet-sdk-2.1.403-linux-arm.tar.gz
mkdir -p /opt/dotnet 
tar zxf dotnet-sdk-2.1.403-linux-arm.tar.gz -C /opt/dotnet
ln -s /opt/dotnet/dotnet /usr/local/bin/
rm -f dotnet-sdk-2.1.403-linux-arm.tar.gz

# Download Source
heading 'Downloading Source'
git clone https://github.com/DanDude0/MilwaukeeMakerspacePiFobReader.git

# Build W26 Library
heading 'Building W26 Library'
cd /root/MilwaukeeMakerspacePiFobReader
cp -rf w26reader /opt/
cd /opt/w26reader
chmod 755 build.sh
./build.sh

# Build Application
heading 'Building MmsPiFobReader Application'
cd /root/MilwaukeeMakerspacePiFobReader/MmsPiFobReader
dotnet publish -c Release -o /opt/MmsPiFobReader

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

# Improve readability of tiny console
heading 'Setting Console Font'
cat > /etc/default/console-setup << 'END'
# CONFIGURATION FILE FOR SETUPCON

# Consult the console-setup(5) manual page.

ACTIVE_CONSOLES="/dev/tty[1-6]"

CHARMAP="UTF-8"

CODESET="guess"
FONTFACE="Terminus"
FONTSIZE="6x12"

VIDEOMODE=

# The following is an example how to use a braille font
# FONT='lat9w-08.psf.gz brl-8x8.psf'
END

# Disable LEDs
heading 'Disabling LEDs'
cat > /etc/rc.local << 'END'
#!/bin/sh -e
#
# rc.local
#
# This script is executed at the end of each multiuser runlevel.
# Make sure that the script will "exit 0" on success or any other
# value on error.
#
# In order to enable or disable this script just change the execution
# bits.
#
# By default this script does nothing.

# Print the IP address
_IP=$(hostname -I) || true
if [ "$_IP" ]; then
  printf "My IP address is %s\n" "$_IP"
fi

echo none >/sys/class/leds/led0/trigger
echo none >/sys/class/leds/led1/trigger
echo 0 >/sys/class/leds/led0/brightness
echo 0 >/sys/class/leds/led1/brightness

exit 0
END

echo 'Install completed, reboot to start reader';