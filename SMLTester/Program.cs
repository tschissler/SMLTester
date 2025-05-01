using SMLTester;
using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;

public class Program
{
    private static List<byte> globalBuffer = new List<byte>();

    static void Main()
    {
        //SerialPort mySerialPort = new SerialPort("/dev/ttyUSB0");
        SerialPort mySerialPort = new SerialPort("com10");

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

        //Extrahiere Datenpakete
        var data = SMLParser.Parse(globalBuffer);
        if (data != null)
        {
            Console.WriteLine($"{"Manufacturer",-15} {"Device-ID",-25} {"Consumption1",15} {"Consumption2",15} {"ConsumptionTotal",20} {"Feed1",10} {"Feed2",10} {"FeedTotal",10} {"Power",10}");
            Console.WriteLine($"{data.ManufacturerId,-15} {data.DeviceId,-25} {data.ConsumptionEnergy1,15} {data.ConsumptionEnergy2,15} {data.ConsumptionEnergyTotal,20} {data.FeedEnergy1,10} {data.FeedEnergy2,10} {data.FeedEnergyTotal,10} {data.EffectivePower,10}");
        }
    }
}

