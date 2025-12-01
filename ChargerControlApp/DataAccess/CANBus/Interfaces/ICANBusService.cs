using ChargerControlApp.DataAccess.CANBus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ChargerControlApp.DataAccess.CANBus.Interfaces
{
    public interface ICANBusService
    {
        //Task<CanMessage?> ReceiveAsync(int timeoutMs);
        //Task SendAsync(CanMessage message);
        public void SendCommand(byte[] data, uint canid);
        public byte[] ReceiveMessage();
        public void ClearCANBuffer();
    }
}
