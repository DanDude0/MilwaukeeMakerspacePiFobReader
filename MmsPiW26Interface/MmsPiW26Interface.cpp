#include <gpiod.h>
#include <stdio.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>

using namespace std;

int main(int argc, char **argv)
{
	const int d0Line = 199;
	const int d1Line = 198;
	const char *pipePath = "/tmp/MmsPiFobReaderKeypad";
	
	int pipe;
	struct gpiod_chip *chip;
	struct gpiod_line_bulk bulk;
	struct gpiod_line_bulk event_bulk;
	struct gpiod_line *line;
	int oldValues[2];
	int values[2];
	int buffer = 0;
	int bitCount = 0;
	int quietCount = 0;
	
	if (access(pipePath, F_OK))
		if (mkfifo(pipePath, 0666))
			return -1;

	pipe = open(pipePath, O_NONBLOCK | O_RDWR);
	
	if (pipe == -1)
		return -2;

	chip = gpiod_chip_open("/dev/gpiochip0");
	
	if (!chip)
		return -3;

	gpiod_line_bulk_init(&bulk);

	line = gpiod_chip_get_line(chip, d0Line);
	gpiod_line_bulk_add(&bulk, line);
	
	line = gpiod_chip_get_line(chip, d1Line);
	gpiod_line_bulk_add(&bulk, line);

	if (gpiod_line_request_bulk_input(&bulk, "MmsPiFobReader"))
		return -4;

	for (int i = 0; i < 10001; i++)
	{
		if (gpiod_line_get_value_bulk(&bulk, values))
			return -5;
		
		if (values[0] == 0 && oldValues[0] == 1) {
			buffer <<= 1;
			bitCount += 1;
			quietCount = 0;
		}
		
		if (values[1] == 0 && oldValues[1] == 1) {
			buffer <<= 1;
			buffer += 1;
			bitCount += 1;
			quietCount = 0;
		}
	
		oldValues[0] = values[0];
		oldValues[1] = values[1];

		if (i == 10000) {
			i = 0;
			quietCount += 1;

			if (quietCount > 2 && bitCount > 0) {
				if (bitCount == 8) {
					buffer &= 0xF;
					dprintf(pipe, "%X\n", buffer);
				}
				else if (bitCount == 26) {
					buffer >>= 1;
					dprintf(pipe, "%06X\n", buffer);
				}

				quietCount = 0;
				buffer = 0;
				bitCount = 0;
			}
		}
	}

	return -6;
}
