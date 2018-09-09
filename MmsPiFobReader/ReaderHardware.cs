using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MmsPiFobReader
{
    public static class ReaderHardware
    {
        static ReaderHardware()
        {
            ReadW26.Initalize();
            WiringPi.pinMode(24, 1); // LED
            WiringPi.pinMode(25, 1); // Beeper
            WiringPi.pinMode(26, 1); // Equipment Trigger
            WiringPi.pinMode(27, 1); // Equipment Trigger
        }

        // TODO, Port C++ W26 library to C# remove dependancy
        public static string Read()
        {
            return ReadW26.Read();
        }

        public static void Login()
        {
            WiringPi.digitalWrite(24, 1);
            WiringPi.digitalWrite(25, 0);
            WiringPi.digitalWrite(26, 0);
            WiringPi.digitalWrite(27, 0);
        }

        public static void Logout()
        {
            if (warningThread != null)
                warningThread.Abort();

            WiringPi.digitalWrite(24, 0);
            WiringPi.digitalWrite(25, 0);
            WiringPi.digitalWrite(26, 1);
            WiringPi.digitalWrite(27, 1);
        }

        public static void Warn(int seconds)
        {
            if (seconds < 60 && seconds > 1)
                WiringPi.digitalWrite(24, seconds % 2 );

            if (seconds > 45 || seconds < 1)
                return;
            else if (seconds > 30)
                WarningLength = 10;
            else
            {
                WarningLength = 510 - (int)(Math.Log(seconds) * 147);
            }

            warningThread = new Thread(WarnThread);
            warningThread.Start();
        }

        private static Thread warningThread;
        private static int WarningLength;

        private static void WarnThread()
        {
            WiringPi.digitalWrite(25, 1);

            Thread.Sleep(WarningLength);

            WiringPi.digitalWrite(25, 0);
        }
    }
}
