using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;

public class Program
{
    private static List<byte> globalBuffer = new List<byte>();

    static void Main()
    {
        SMLMeter smartMeter = new SMLMeter("/dev/ttyUSB0");
        Console.WriteLine("Connected to Meter " + smartMeter.MeterID);

        Console.WriteLine("Asking for programming mode...");
        smartMeter.SetMode(true);
        Console.WriteLine("Sending password");
        if (smartMeter.Login())
        {
            Console.WriteLine("Login successfull");
        }
        else
        {
            Console.WriteLine("Smart Meter did not accept password.");
        }

        while (true)
        {
            Console.Clear();

            Console.WriteLine("Voltage  : " + smartMeter.GetVoltage() + " V");
            Console.WriteLine("Frequency: " + smartMeter.GetFrequency() + " Hz");
            Console.WriteLine("Current  : " + smartMeter.GetCurrent() + " A");
            Console.WriteLine("Power    : " + smartMeter.GetPower() + " W");

            System.Threading.Thread.Sleep(1000);
        }

        smartMeter.Logout();
        SerialPort mySerialPort = new SerialPort("/dev/ttyUSB0");

        mySerialPort.BaudRate = 9600;
        mySerialPort.Parity = Parity.None;
        mySerialPort.StopBits = StopBits.One;
        mySerialPort.DataBits = 8;
        mySerialPort.Handshake = Handshake.None;

        mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

        mySerialPort.Open();

        Console.WriteLine("Press any key to continue...");
        Console.WriteLine();
        Console.ReadKey();
        mySerialPort.Close();
    }

    private static void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        int bytesToRead = sp.BytesToRead;
        byte[] buffer = new byte[bytesToRead];
        sp.Read(buffer, 0, bytesToRead);
        globalBuffer.AddRange(buffer);
        Console.Write("Data Received: ");
        foreach (byte b in buffer)
        {
            Console.Write(b.ToString("X2") + " ");
        }
        Console.WriteLine();
    }

    public class SmlParser
    {
        private readonly byte[] StartSequence = new byte[] { 0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01 };
        private readonly byte[] EndSequence = new byte[] { 0x1B, 0x1B, 0x1B, 0x1B, 0x1A };

        public List<byte[]> Parse(byte[] smlData)
        {
            List<byte[]> messages = new List<byte[]>();
            int startIndex = 0;

            while ((startIndex = FindSequence(smlData, StartSequence, startIndex)) != -1)
            {
                int endIndex = FindSequence(smlData, EndSequence, startIndex);
                if (endIndex == -1) break;

                int messageLength = endIndex - startIndex + EndSequence.Length;
                byte[] message = new byte[messageLength];
                Array.Copy(smlData, startIndex, message, 0, messageLength);
                messages.Add(message);

                startIndex = endIndex + EndSequence.Length;
            }

            return messages;
        }

        private int FindSequence(byte[] source, byte[] sequence, int start)
        {
            for (int i = start; i < source.Length - sequence.Length + 1; i++)
            {
                if (source.Skip(i).Take(sequence.Length).SequenceEqual(sequence))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

