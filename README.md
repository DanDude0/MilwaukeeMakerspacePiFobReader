# MilwaukeeMakerspacePiFobReader

## I want to test this on a desktop
This is pretty easy to do on any common desktop platform:

### Windows
#### Recommended - Install the full Visual Studio IDE for a nice development experince
Get the installer here:

<https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&rel=15>

Be sure to select the `.Net Core cross-platform development` workload.

#### Alternate - Just install the necessary SDKs
Get the installer for the .Net Core 2.1 SDK here:

<https://www.microsoft.com/net/download/thank-you/dotnet-sdk-2.1.402-windows-x64-installer>

### Mac
Get the installer for the .Net Core 2.1 SDK here:

<https://www.microsoft.com/net/download/thank-you/dotnet-sdk-2.1.402-windows-x64-installer>

Install the SDL2 library:
	
	brew install sdl2

### Linux
Instructions good for anything Debian/Ubuntu based. Translating for other package managers of your choice should be trivial:

Install .Net Core 2.1 SDK:

Directions from here:

<https://www.microsoft.com/net/download/linux-package-manager/debian9/sdk-current>

	wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
	mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
	wget -q https://packages.microsoft.com/config/debian/9/prod.list
	mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
	chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
	chown root:root /etc/apt/sources.list.d/microsoft-prod.list

	apt-get update
	apt-get install dotnet-sdk-2.1

Install the SDL2 library:
	
	apt install libsdl2-2.0-0

### Now lets run the damn thing already

#### Use the command line (good for any of the above platforms):

Checkout the repository to a folder on your machine:

	git clone https://github.com/DanDude0/MilwaukeeMakerspacePiFobReader

Switch to the project directory:

	cd MilwaukeeMakerspacePiFobReader/MmsPiFobReader
	
Build the project:

	dotnet build
	
And run the project:

	dotnet run

You'll probably get this message the first time it runs:

	! ERROR !
	Reader Id is not
	set

To fix this you need to give this install an integer reader id, that corresponds to one on the authentication server that it will be connecting to. You do this by setting it in a simple text file named `readerid.txt` saved in the working directory. This is the only machine side configuration that is done on the reader. Everything else gets configured by the API server upon first connection. As an example, lets use 5, which is conviently configured as `Test Reader` at Milwaukee Makerspace.

	echo '5' > readerid.txt
	
Now lets try again:

	dotnet run
	
If it loaded correctly, you are ready to test and develop! Otherwise proceed:

"You stupid fuck, it's still broken!"  - You probably see this error:

	! ERROR !
	Cannot reach
	server
	
If you aren't working inside the actual Milwaukee Makerspace building, you'll need to be running your own copy of the API server on your local network for the reader to connect to. That is beyond the scope of this tutorial. Find out about that project and over here:

<https://github.com/DanDude0/MilwaukeeMakerspaceApi>

#### Or use Visual Studio on Windows:

If you elected to install Visual Studio you can just open the `MmsPiFobReader.sln` solution file and run from within the IDE. You'll still need to set the `readerid.txt` file and have access to an instance API server.

## I want to setup an actual hardware reader

### Parts List

There is a spreadsheet here containing parts, quantities, prices, and links to parts for purchase

<https://docs.google.com/spreadsheets/d/1saBPHnn_E8FyzVhVKWeM24Enc3zIGl8CUS3w7r8rCs0/edit?usp=sharing>

### Custom Circuit Board

This is the schematic and layout for an interface board to go link the various hardware components

<https://easyeda.com/Dan_Dude/Pi-Fob-Reader>

### Case

TODO: Add plans for housing

### Install OS

Write `Raspbian Lite` onto Micro SD card using your desktop computer as documented here:

<https://www.raspberrypi.org/downloads/raspbian/>

<https://www.raspberrypi.org/documentation/installation/installing-images/README.md>

### Configure OS for headless operation

Before you install the SD card into the Pi for the first time, we need to configure a few things.

Open up the root directory of the just created `boot` partition on the card.

Create a blank text file called `ssh`. This will enable the ssh server so that you can control the OS without a keyboard attached.
	
Next create a text file called `wpa_supplicant.conf`. Add this text to the file, substituting in the SSID and Password of your WiFi Network. This will automatically connect the WiFi on boot.

	ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
	update_config=1
	country=US

	network={
		ssid="«your_SSID»"
		psk="«your_PSK»"
		key_mgmt=WPA-PSK
	}
	
Next we have to configure the 3.5" screen as a device and point the console at it. 

Open up the `cmdline.txt` file. Locate the `quiet` arguement:

	quiet
	
Replace `quiet` with the following arguements:

	fbcon=map:11 consoleblank=0 vt.global_cursor_default=0
	
Next, open the `config.txt` file. Go to the bottom of the file. Change `dtparam=audio` to `off`

	dtparam=audio=off
	
Add the following lines to the bottom of the file:

	# Limit power consumption and improve stability
	arm_freq=600
	gpu_freq=200
	core_freq=200
	sdram_freq=400
	over_voltage_sdram=0
	dtoverlay=pi3-disable-bt
	enable_uart=0

	#Driver for screen
	dtparam=spi=on
	dtoverlay=piscreen,rotate=270,speed=24000000

Now you can insert the SD card into your Raspberry Pi.

### Assembly

TODO: Document assembly

### Install Client Software

TODO: Document installation

## I want to develop on a desktop machine, and deploy to a reader easily

TODO: Flesh this out a wee bit more :)

This is fun, start by setting up a desktop development environment as described above.

Then install PuTTY

Use the `deploy.bat` script to push out new builds.