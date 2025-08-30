using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChargerControlApp.DataAccess.CANBus.Models
{
    public class CanId
    {

        public uint Value { get; set; }
        public bool IsExtended { get; set; } = false; // 判斷是否為擴展型 ID（29 位元）
        public uint ToRaw()
        {
            return IsExtended ? (Value | 0x80000000) : Value;
        }

        public static CanId FromRaw(uint raw)
        {
            return new CanId
            {
                Value = raw & 0x1FFFFFFF,
                IsExtended = (raw & 0x80000000) != 0
            };
        }

        public override string ToString()
        {
            return $"{(IsExtended ? "Ext" : "Std")}-{Value:X}";
        }
    }

}
