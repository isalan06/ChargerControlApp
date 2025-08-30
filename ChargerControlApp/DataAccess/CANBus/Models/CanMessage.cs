using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChargerControlApp.DataAccess.CANBus.Models
{
    public class CanMessage
    {
        public CanId Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte DLC => (byte)(Data?.Length ?? 0);
    }
}
