using System.Collections.Generic;

namespace ChargerControlApp.DataAccess.GPIO.Models
{
    /// <summary>
    /// 表示 Raspberry Pi 4 單一 GPIO 腳位的輸入資訊
    /// </summary>
    public class GPIOInfo
    {
        /// <summary>
        /// 實體腳位編號（1~40）
        /// </summary>
        public int PhysicalPinNumber { get; set; }

        /// <summary>
        /// BCM 編號（如 2, 3, 4, ...，非 GPIO 腳位為 -1）
        /// </summary>
        public int BCMNumber { get; set; }

        /// <summary>
        /// 腳位名稱（如 GPIO2, 3.3V, GND 等）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否為輸入腳位
        /// </summary>
        public bool IsInput { get; set; }

        /// <summary>
        /// 目前腳位狀態（true: High, false: Low）
        /// </summary>
        public bool Value { get; set; }

        /// <summary>
        /// 取得 Raspberry Pi 4 的預設 40Pin 腳位對應表
        /// </summary>
        public static List<GPIOInfo> GetDefaultPinMapping()
        {
            return new List<GPIOInfo>
            {
                new GPIOInfo { PhysicalPinNumber = 1,  BCMNumber = -1, Name = "3.3V",    IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 2,  BCMNumber = -1, Name = "5V",      IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 3,  BCMNumber = 2,  Name = "GPIO2",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 4,  BCMNumber = -1, Name = "5V",      IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 5,  BCMNumber = 3,  Name = "GPIO3",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 6,  BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 7,  BCMNumber = 4,  Name = "GPIO4",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 8,  BCMNumber = 14, Name = "GPIO14",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 9,  BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 10, BCMNumber = 15, Name = "GPIO15",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 11, BCMNumber = 17, Name = "GPIO17",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 12, BCMNumber = 18, Name = "GPIO18",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 13, BCMNumber = 27, Name = "GPIO27",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 14, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 15, BCMNumber = 22, Name = "GPIO22",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 16, BCMNumber = 23, Name = "GPIO23",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 17, BCMNumber = -1, Name = "3.3V",    IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 18, BCMNumber = 24, Name = "GPIO24",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 19, BCMNumber = 10, Name = "GPIO10",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 20, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 21, BCMNumber = 9,  Name = "GPIO9",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 22, BCMNumber = 25, Name = "GPIO25",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 23, BCMNumber = 11, Name = "GPIO11",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 24, BCMNumber = 8,  Name = "GPIO8",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 25, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 26, BCMNumber = 7,  Name = "GPIO7",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 27, BCMNumber = 0,  Name = "ID_SD",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 28, BCMNumber = 1,  Name = "ID_SC",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 29, BCMNumber = 5,  Name = "GPIO5",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 30, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 31, BCMNumber = 6,  Name = "GPIO6",   IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 32, BCMNumber = 12, Name = "GPIO12",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 33, BCMNumber = 13, Name = "GPIO13",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 34, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 35, BCMNumber = 19, Name = "GPIO19",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 36, BCMNumber = 16, Name = "GPIO16",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 37, BCMNumber = 26, Name = "GPIO26",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 38, BCMNumber = 20, Name = "GPIO20",  IsInput = true  },
                new GPIOInfo { PhysicalPinNumber = 39, BCMNumber = -1, Name = "GND",     IsInput = false },
                new GPIOInfo { PhysicalPinNumber = 40, BCMNumber = 21, Name = "GPIO21",  IsInput = true  }
            };
        }
    }
}
