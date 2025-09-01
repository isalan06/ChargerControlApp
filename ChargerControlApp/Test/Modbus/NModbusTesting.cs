using Modbus.Data;
using Modbus.Device;
using Modbus.IO;
using System.Diagnostics.Metrics;
using System.IO.Ports;


namespace ChargerControlApp.Test.Modbus
{
    public class NModbusTesting
    {
        public void Test() 
        {
            using (SerialPort sp = new SerialPort("/dev/ttySC0"))
            {

                sp.Parity = Parity.Even;
                sp.BaudRate = 115200;
                try
                {
                    string portName = sp.PortName;

                    Console.WriteLine("Ver1.3");


#if DEBUG
                    portName = "COM1";
#else
                    portName = "/dev/ttySC0";
#endif

                    sp.PortName = portName;
                    sp.Open();
                    Console.WriteLine("Serial Port Open!!!!");
                    var port = ModbusSerialMaster.CreateRtu(sp);
                    port.Transport.ReadTimeout = 300;
                   

                    byte slaveId = 1; // The Modbus slave ID of your device
                    ushort startAddress = 0x7C; // The starting register address to read
                    ushort numberOfPoints = 4; // The number of registers to read

                    //ushort[] holdingRegisters = port.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);

                    //Console.WriteLine($"Read Holding Registers from Slave ID {slaveId}, starting at address {startAddress}:");
                    //for (int i = 0; i < holdingRegisters.Length; i++)
                    //{
                    //    Console.WriteLine($"Register {startAddress + i}: {holdingRegisters[i]}");
                    //}

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Serial Port Error!!!!");
                }
                finally
                {
                    Console.WriteLine("Serial Port Closing!!!!");
                    sp.Close();
                    Console.WriteLine("Serial Port Close!!!!");
                }
            }



        }
    }
}
