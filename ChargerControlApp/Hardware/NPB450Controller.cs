using ChargerControlApp.DataAccess;
using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Linux;
using ChargerControlApp.DataAccess.CANBus.Models;
using ChargerControlApp.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;


namespace ChargerControlApp.Hardware
{
    public class NPB450Controller
    {
        public static int NPB450ControllerInstnaceMaxNumber = 8; // TODO: 之後改成設定檔
        public static bool ChargerUseAsync = false; // TODO: 之後改成設定檔
        private readonly ILogger<NPB450Controller> _logger; 

        private readonly ICANBusService _canBusService;
        private readonly ChargingStationStateMachine _chargingStationStateMachine;

        private static uint rpb1700DeviceID = 3; // 此參數為固定測值，後續不使用該數值
        private static uint rpb1700MessageID = 0x000C0100; 
        private readonly uint canID = rpb1700DeviceID | rpb1700MessageID; // 此參數為固定測值，後續不使用該數值
        //充電器對控制器的MessageID=0x000C00XX
        //控制器對充電器的MessageID=0x000C01XX
        //控制器對充電氣廣播的MessageID=0x000C01FF
        public uint deviceID { get; private set; } = 0; // 讓每一台NPB450有不同的ID
        public uint deviceCanID { get { return (deviceID | rpb1700MessageID); } } // 讓每一台NPB450有不同的CANID

        private double Voltage = 0;
        private double Current = 0;
        public FAULT_STATUS_Union FAULT_STATUS;
        public CHG_STATUS_Union CHG_STATUS;
        public bool IsUsed { get; set; } = false;
        private bool startChargingTrigger = false;
        private bool stopChargingTrigger = false;

        // timeout
        private bool isReadError = false;
        private long timeoutMilliseconds = 1000;
        private Stopwatch stopwatch = new Stopwatch();  

        public enum CanbusReadCommand : ushort
        {
            OPERATION = 0x0000,
            VOUT_SET = 0x0020,
            IOUT_SET = 0x0030,
            FAULT_STATUS = 0x0040,
            READ_VIN = 0x0050,
            READ_VOUT = 0x0060,
            READ_IOUT = 0x0061,
            READ_TEMPERATURE_1 = 0x0062,
            MFR_ID_B0B5 = 0x0080,
            MFR_ID_B6B11 = 0x0081,
            MFR_MODEL_B0B5 = 0x0082,
            MFR_MODEL_B6B11 = 0x0083,
            MFR_REVISION_B0B5 = 0x0084,
            MFR_LOCATION_B0B2 = 0x0085,
            MFR_DATE_B0B5 = 0x0086,
            MFR_SERIAL_B0B5 = 0x0087,
            MFR_SERIAL_B6B11 = 0x0088,
            CURVE_CC = 0x00B0,
            CURVE_CV = 0x00B1,
            CURVE_FV = 0x00B2,
            CURVE_TC = 0x00B3,
            CURVE_CONFIG = 0x00B4,
            CURVE_CC_TIMEOUT = 0x00B5,
            CURVE_CV_TIMEOUT = 0x00B6,
            CURVE_FV_TIMEOUT = 0x00B7,
            CHG_STATUS = 0x00B8,
            CHG_RST_VBAT = 0x00B9,
            SCALING_FACTOR = 0x00c0,
            SYSTEM_STATUS = 0x00C1,
            SYSTEM_CONFIG = 0x00C2
        }

        public enum CanbusWriteCommand : ushort
        {
            OPERATION = 0x0000,
            VOUT_SET = 0x0020,
            IOUT_SET = 0x0030,
            MFR_LOCATION_B0B2 = 0x0085,
            MFR_DATE_B0B5 = 0x0086,
            MFR_SERIAL_B0B5 = 0x0087,
            MFR_SERIAL_B6B11 = 0x0088,
            CURVE_CC = 0x00B0,
            CURVE_CV = 0x00B1,
            CURVE_FV = 0x00B2,
            CURVE_TC = 0x00B3,
            CURVE_CONFIG = 0x00B4,
            CURVE_CC_TIMEOUT = 0x00B5,
            CURVE_CV_TIMEOUT = 0x00B6,
            CURVE_FV_TIMEOUT = 0x00B7,
            CHG_RST_VBAT = 0x00B9,
            SYSTEM_CONFIG = 0x00C2
        }

        //OPERATION = 0x0000,
        //    VOUT_SET = 0x0020,
        //    IOUT_SET = 0x0030,
        //    FAULT_STATUS = 0x0040,
        //    READ_VIN = 0x0050,
        //    READ_VOUT = 0x0060,
        //    READ_IOUT = 0x0061,
        //    READ_TEMPERATURE_1 = 0x0062,
        //    MFR_ID_B0B5 = 0x0080,
        //    MFR_ID_B6B11 = 0x0081,
        //    MFR_MODEL_B0B5 = 0x0082,
        //    MFR_MODEL_B6B11 = 0x0083,
        //    MFR_REVISION_B0B5 = 0x0084,
        //    MFR_LOCATION_B0B2 = 0x0085,
        //    MFR_DATE_B0B5 = 0x0086,
        //    MFR_SERIAL_B0B5 = 0x0087,
        //    MFR_SERIAL_B6B11 = 0x0088,
        //    CURVE_CC = 0x00B0,
        //    CURVE_CV = 0x00B1,
        //    CURVE_FV = 0x00B2, 
        //    CURVE_TC = 0x00B3,
        //    CURVE_CONFIG = 0x00B4,
        //    CURVE_CC_TIMEOUT = 0x00B5,
        //    CURVE_CV_TIMEOUT = 0x00B6,
        //    CURVE_FV_TIMEOUT = 0x00B7,
        //    CHG_STATUS = 0x00B8,
        //    CHG_RST_VBAT = 0x00B9,
        //    SCALING_FACTOR = 0x00c0,
        //    SYSTEM_STATUS = 0x00C1,
        //    SYSTEM_CONFIG = 0x00C2


        /*FAULT_STATUS(0x00 40)
        Low Byte
        Bit 1 OTP: Over temperature protection
            0 = Internal temperature normal
            1 = Internal temperature abnormal
        Bit 2 OVP: Output over voltage protection
            0 = Output voltage normal
            1 = Output voltage protected
        Bit 3 OLP: Output over current protection
            0 = Output current normal
            1 = Output current protected
        Bit 4 SHORT: Output short circuit protection
            0 = Shorted circuit do not exist
            1 = Output shorted circuit protected
        Bit 5 AC_FAIL: AC abnormal flag
            0 = AC main normal
            1 = AC abnormal protection
        Bit6 OP_OFF: Output status
            0 = Output turned on
            1 = Output turned off
        Bit7 HI_TEMP: Internal high temperature protection
            0 = Internal temperature normal
            1 = Internal temperature abnormal
        */
        [StructLayout(LayoutKind.Explicit)]
        public struct FAULT_STATUS_Union
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;
                public bool Bit0 => (Value & (1 << 0)) != 0;
                public bool OTP => (Value & (1 << 1)) != 0;
                public bool OVP => (Value & (1 << 2)) != 0;
                public bool OLP => (Value & (1 << 3)) != 0;
                public bool SHORT => (Value & (1 << 4)) != 0;
                public bool AC_FAIL => (Value & (1 << 5)) != 0;
                public bool OP_OFF => (Value & (1 << 6)) != 0;
                public bool HI_TEMP => (Value & (1 << 7)) != 0;
                public bool Bit8 => (Value & (1 << 8)) != 0;
                public bool Bit9 => (Value & (1 << 9)) != 0;
                public bool Bit10 => (Value & (1 << 10)) != 0;
                public bool Bit11 => (Value & (1 << 11)) != 0;
                public bool Bit12 => (Value & (1 << 12)) != 0;
                public bool Bit13 => (Value & (1 << 13)) != 0;
                public bool Bit14 => (Value & (1 << 14)) != 0;
                public bool Bit15 => (Value & (1 << 15)) != 0;
            }
        }

        /*CHG_STATUS (0x00B8)
        High byte
        Bit 2 NTCER:Temperature compensation status
            0＝NO short-circuit in the circuitry of temperature compensation
            1＝The circuitry of temperature compensation has short-circuited
        Bit 3 BTNC:Battery detection
            0＝Battery detected
            1= No battery detected
        Bit 5 CCTOF:Time out flag of constant current mode
            0＝NO time out in constant current mode
            1= Constant current mode time out
        Bit 6 CVTOF:Time out flag of constant voltage mode
            0= NO time out in constant voltage mode
            1= Constant voltage mode time out
        Bit 7 FVTOF:Time out flag of float mode
            0＝NO time out in float mode
            ＝1 Float mode timed out
        Low byte
        Bit 0 FULLM:Fully charged status
            0 =Not fully charged
            1 =Fully charged
        Bit 1 CCM:Constant current mode status
            0＝The charger NOT in constant current mode
            1=The charger in constant current mode
        Bit 2 CVM:Constant voltage mode status
            0= The charge NOT in constant voltage mode
            1= The charge in constant voltage mode
        Bit 3  FVM:Float mode status
            0＝The charger NOT in float mode
            1＝The charger in float mode
        Bit 6  WAKEUP_STOP: Wake up finished
            0 = Wake up finished＝
            1 = Wake up unfinished
         */
        [StructLayout(LayoutKind.Explicit)]
        public struct CHG_STATUS_Union
        {
            [FieldOffset(0)] public ushort Data;  // ✅ 這是實例欄位，不是 static

            [FieldOffset(0)] public BitField Bits; // ✅ 確保 `Bits` 和 `Data` 共用記憶體

            [StructLayout(LayoutKind.Sequential)]
            public struct BitField
            {
                public ushort Value;

                public bool FULLM => (Value & (1 << 0)) != 0;
                public bool CCM => (Value & (1 << 1)) != 0;
                public bool CVM => (Value & (1 << 2)) != 0;
                public bool FVM => (Value & (1 << 3)) != 0;
                public bool Bit4 => (Value & (1 << 4)) != 0;
                public bool Bit5 => (Value & (1 << 5)) != 0;
                public bool WAKEUP_STOP => (Value & (1 << 6)) != 0;
                public bool Bit7 => (Value & (1 << 7)) != 0;
                public bool Bit8 => (Value & (1 << 8)) != 0;
                public bool Bit9 => (Value & (1 << 9)) != 0;
                public bool NTCER => (Value & (1 << 10)) != 0;
                public bool BTNC => (Value & (1 << 11)) != 0;
                public bool Bit12 => (Value & (1 << 12)) != 0;
                public bool CCTOF => (Value & (1 << 13)) != 0;
                public bool CVTOF => (Value & (1 << 14)) != 0;
                public bool FVTOF => (Value & (1 << 15)) != 0;
            }
        }

        //polling
        private bool _running = true;

        //public NPB1700Controller(IServiceProvider serviceProvider, SocketCanBusService2 canBusService)   // TODO: canbus service 改DI 或POLLING功能拆到CANBUS
        //public NPB1700Controller(IServiceProvider serviceProvider)//, SocketCANBusService canBusService)   // TODO: canbus service 改DI 或POLLING功能拆到CANBUS
        //{
        //    //if (Services == null)
        //    //    throw new InvalidOperationException("ServiceProvider 尚未初始化");
        //    // 使用 DI 取得服務
        //    _chargingStationStateMachine = serviceProvider.GetRequiredService<ChargingStationStateMachine>();
        //    _canBusService = serviceProvider.GetRequiredService<SocketCANBusService>();
        //    //_canBusService = canBusService; //TODO: 要改DI
        //    
        //}
        public NPB450Controller(ChargingStationStateMachine stateMachine, ICANBusService canBusService, int id, ILogger<NPB450Controller> logger)
        {
            _chargingStationStateMachine = stateMachine;
            _canBusService = canBusService;
            deviceID = (uint)id;
            _logger = logger;
            //Task.Run(() => PollingLoop());
        }



        ////public class NPB1700Controller
        //{
        //    private readonly ConcurrentDictionary<string, object> _cache = new();

        public async void PollingOnce()
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-Start");
                _canBusService.ClearCANBuffer();
                // 這裡是實際和硬體通訊的地方
                this.Voltage = await GetVoltage();// TODO: Async有問題，還沒檢查，之後要改 await GetVoltageAsync();
                this.Current = await GetCurrent();
                this.CHG_STATUS = await GetCHG_STATUS();
                this.FAULT_STATUS = await GetFAULT_STATUS();
                if (this.startChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StartCharging()");
                    this.startChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x01;
                    _canBusService.SendCommand(send, deviceCanID);
                }
                if (this.stopChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StopCharging()");
                    this.stopChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x00;
                    _canBusService.SendCommand(send, deviceCanID);
                }
                _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-End");
            }
            else
            {
                // 模擬資料
                if (this.IsUsed)
                {
                    this.Voltage += 0.1 + 0.005 * (double)deviceID;
                    this.Current += 0.015 + 0.001 * (double)deviceID;
                    if (this.Voltage >= 250.0) this.Voltage = 0.0;
                    if (this.Current >= 5.0) this.Current = 0.0;
                }
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-PollingOnce()");

                if (this.startChargingTrigger)
                {
                    this.startChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StartCharging()");
                }

                if(this.stopChargingTrigger)
                {
                    this.stopChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StopCharging()");
                }
            }
        }


        public async void PollingOnceSync()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-Start");
                _canBusService.ClearCANBuffer();
                // 這裡是實際和硬體通訊的地方

                // Voltage
                double voltageValue = await GetVoltage_Sync();
                if (isReadError) return;
                if (voltageValue != -1)
                    this.Voltage = voltageValue;

                Task.Delay(100).Wait(); // 讀取間隔

                // Current
                double currentValue = await GetCurrent_Sync();
                if(isReadError) return;
                if (currentValue != -1)
                    this.Current = currentValue;

                Task.Delay(100).Wait(); // 讀取間隔

                // CHG_STATUS
                NPB450Controller.CHG_STATUS_Union cHG_STATUS_Union = await GetCHG_STATUS_Sync();
                if (isReadError) return;
                this.CHG_STATUS = cHG_STATUS_Union;

                Task.Delay(100).Wait(); // 讀取間隔


                NPB450Controller.FAULT_STATUS_Union fAULT_STATUS_Union = await GetFAULT_STATUS_Sync();
                if (isReadError) return;
                this.FAULT_STATUS = fAULT_STATUS_Union;

                Task.Delay(100).Wait(); // 讀取間隔

                if (this.startChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StartCharging()");
                    this.startChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x01;
                    _canBusService.SendCommand(send, deviceCanID);

                    Task.Delay(100).Wait(); // 讀取間隔
                }
                if (this.stopChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StopCharging()");
                    this.stopChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x00;
                    _canBusService.SendCommand(send, deviceCanID);

                    Task.Delay(100).Wait(); // 讀取間隔
                }
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-End");
            }
            else
            {
                // 模擬資料
                if (this.IsUsed)
                {
                    this.Voltage += 0.1 + 0.005 * (double)deviceID;
                    this.Current += 0.015 + 0.001 * (double)deviceID;
                    if (this.Voltage >= 250.0) this.Voltage = 0.0;
                    if (this.Current >= 5.0) this.Current = 0.0;
                }
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-PollingOnce()");

                if (this.startChargingTrigger)
                {
                    this.startChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StartCharging()");
                }

                if (this.stopChargingTrigger)
                {
                    this.stopChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StopCharging()");
                }
            }
        }

        /// <summary>
        /// TODO: Async有問題，還沒檢查
        /// </summary>
        /// <returns></returns>
        public async Task PollingOnceAsync()
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-Start");
                _canBusService.ClearCANBuffer();
                // 這裡是實際和硬體通訊的地方
                this.Voltage = await GetVoltageAsync();// TODO: Async有問題，還沒檢查，之後要改 await GetVoltageAsync();
                this.Current = await GetCurrentAsync();
                this.CHG_STATUS = await GetCHG_STATUSAsync();
                this.FAULT_STATUS = await GetFAULT_STATUSAsync();
                if (this.startChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StartCharging()");
                    this.startChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x01;
                    _canBusService.SendCommand(send, deviceCanID);
                }
                if (this.stopChargingTrigger)
                {
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-StopCharging()");
                    this.stopChargingTrigger = false;
                    int numberOfDataBytes = 1;
                    byte[] send = new byte[2 + numberOfDataBytes];
                    byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
                    sendBytes.CopyTo(send, 0);
                    send[2] = (byte)0x00;
                    _canBusService.SendCommand(send, deviceCanID);
                }
                _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-End");
            }
            else
            {
                // 模擬資料
                if (this.IsUsed)
                {
                    this.Voltage += 0.1 + 0.005 * (double)deviceID;
                    this.Current += 0.015 + 0.001 * (double)deviceID;
                    if (this.Voltage >= 250.0) this.Voltage = 0.0;
                    if (this.Current >= 5.0) this.Current = 0.0;
                }
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-PollingOnce()");

                if (this.startChargingTrigger)
                {
                    this.startChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StartCharging()");
                }

                if (this.stopChargingTrigger)
                {
                    this.stopChargingTrigger = false;
                    _logger.LogInformation($"NPB450Controller{this.deviceID}-[Windows虛擬]-StopCharging()");
                }
            }
        }
        public double GetCachedVoltage() { return this.Voltage; }
        public double GetCachedCurrent() { return this.Current; }
        public CHG_STATUS_Union GetCachedCHG_STATUS() { return this.CHG_STATUS; }
        public FAULT_STATUS_Union GetCachedFAULT_STATUS() { return this.FAULT_STATUS; }

        private async Task<double> GetVoltage()
        {
            _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetVoltage()-Start");
            byte[] VoltageBytes = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.READ_VOUT);
            _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetVoltage()-Bytes:{BitConverter.ToString(VoltageBytes)}");
            ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
            _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetVoltage()-{Voltage}");
            return (double)Voltage / 100;
        }
        private async Task<double> GetVoltage_Sync()
        {
            isReadError = false;
            ChargersReader.ChargerIndex = (int)this.deviceID;
            ChargersReader.ChargerCommandData = 0; // 讀取電壓

            await GetStatusFromDevice_OnlySend(NPB450Controller.CanbusReadCommand.READ_VOUT); // 只送不接收
            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Restart(); // 重啟計時器
            while (true)
            {
                //if (CheckChargerReaderResponse()) // 有收到回應
                if(((ChargersReader.ChargerResponseIndex == (int)this.deviceID)
                && (ChargersReader.ChargerResponseData == 0)))
                {
                    byte[]? VoltageBytes = ChargersReader.ReceivedCANBusMessage; // 取得收到的資料
                    if (VoltageBytes == null)
                    {
                        isReadError = true;
                        _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetVoltage_Sync()-ReceivedCANBusMessage is null");
                        
                        return -1;
                    }
                    ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
                    return (double)Voltage / 100;
                }
                if (_stopwatch.ElapsedMilliseconds > timeoutMilliseconds) // 超時
                {
                    isReadError = true;
                    _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetVoltage_Sync()-Timeout");
                    Task.Delay(100).Wait();
                    return -1; 
                }
                await Task.Delay(10);
            }
        }

        private async Task<double> GetVoltageAsync()
        {
            byte[] VoltageBytes = await GetStatusFromDeviceAsync(NPB450Controller.CanbusReadCommand.READ_VOUT);
            ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
            return (double)Voltage / 100;
        }
        private async Task<double> GetCurrent()
        {
            byte[] CurrentBytes = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.READ_IOUT);
            ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
            return (double)Current / 100;
        }

        private async Task<double> GetCurrent_Sync()
        {
            isReadError = false;
            ChargersReader.ChargerIndex = (int)this.deviceID;
            ChargersReader.ChargerCommandData = 1; // 讀取電流

            await GetStatusFromDevice_OnlySend(NPB450Controller.CanbusReadCommand.READ_IOUT); // 只送不接收

            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Restart(); // 重啟計時器
            while (true)
            {
                //if (CheckChargerReaderResponse()) // 有收到回應
                if (((ChargersReader.ChargerResponseIndex == (int)this.deviceID)
                && (ChargersReader.ChargerResponseData == 1)))
                {
                    byte[]? CurrentBytes = ChargersReader.ReceivedCANBusMessage; // 取得收到的資料
                    if (CurrentBytes == null)
                    {
                        isReadError = true;
                        _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetCurrent_Sync()-ReceivedCANBusMessage is null");

                        return -1;
                    }
                    ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
                    return (double)Current / 100;
                }
                if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds) // 超時
                {
                    isReadError = true;
                    _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetCurrent_Sync()-Timeout");
                    Task.Delay(100).Wait();
                    return -1;
                }
                await Task.Delay(10);
            }
        }
        private async Task<double> GetCurrentAsync()
        {
            byte[] CurrentBytes = await GetStatusFromDeviceAsync(NPB450Controller.CanbusReadCommand.READ_IOUT);
            ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
            return (double)Current / 100;
        }
        private async Task<CHG_STATUS_Union> GetCHG_STATUS()
        {
            byte[] CHG_STATUS_BYTES = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.CHG_STATUS);
            CHG_STATUS_Union CHG_STATUS = new CHG_STATUS_Union();
            CHG_STATUS.Data = BitConverter.ToUInt16(new byte[] { CHG_STATUS_BYTES[2], CHG_STATUS_BYTES[3] }.ToArray(), 0);
            return CHG_STATUS;
        }

        private async Task<CHG_STATUS_Union> GetCHG_STATUS_Sync()
        {
            isReadError = false;
            ChargersReader.ChargerIndex = (int)this.deviceID;
            ChargersReader.ChargerCommandData = 2; // 讀取CHG_STATUS
            await GetStatusFromDevice_OnlySend(NPB450Controller.CanbusReadCommand.CHG_STATUS); // 只送不接收

            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Restart(); // 重啟計時器
            while (true)
            {
                //if (CheckChargerReaderResponse()) // 有收到回應
                if (((ChargersReader.ChargerResponseIndex == (int)this.deviceID)
                && (ChargersReader.ChargerResponseData == 2)))
                {
                    byte[]? CHG_STATUS_BYTES = ChargersReader.ReceivedCANBusMessage; // 取得收到的資料
                    if (CHG_STATUS_BYTES == null)
                    {
                        isReadError = true;
                        _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetCHG_STATUS_Sync()-ReceivedCANBusMessage is null");
                        return this.CHG_STATUS;
                    }
                    CHG_STATUS_Union CHG_STATUS = new CHG_STATUS_Union();
                    CHG_STATUS.Data = BitConverter.ToUInt16(new byte[] { CHG_STATUS_BYTES[2], CHG_STATUS_BYTES[3] }.ToArray(), 0);
                    return CHG_STATUS;
                }
                if (_stopwatch.ElapsedMilliseconds > timeoutMilliseconds) // 超時
                {
                    isReadError = true;
                    _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetCHG_STATUS_Sync()-Timeout");
                    Task.Delay(100).Wait();
                    return this.CHG_STATUS;
                }
                await Task.Delay(10);
            }
        }
        private async Task<CHG_STATUS_Union> GetCHG_STATUSAsync()
        {
            byte[] CHG_STATUS_BYTES = await GetStatusFromDeviceAsync(NPB450Controller.CanbusReadCommand.CHG_STATUS);
            CHG_STATUS_Union CHG_STATUS = new CHG_STATUS_Union();
            CHG_STATUS.Data = BitConverter.ToUInt16(new byte[] { CHG_STATUS_BYTES[2], CHG_STATUS_BYTES[3] }.ToArray(), 0);
            return CHG_STATUS;
        }

        private async Task<FAULT_STATUS_Union> GetFAULT_STATUS()
        {
            byte[] FAULT_STATUS_BYTES = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.FAULT_STATUS);
            FAULT_STATUS_Union FAULT_STATUS = new FAULT_STATUS_Union();
            FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);
            return FAULT_STATUS;
        }

        private async Task<FAULT_STATUS_Union> GetFAULT_STATUS_Sync()
        {
            isReadError = false;
            ChargersReader.ChargerIndex = (int)this.deviceID;
            ChargersReader.ChargerCommandData = 3; // 讀取FAULT_STATUS
            await GetStatusFromDevice_OnlySend(NPB450Controller.CanbusReadCommand.FAULT_STATUS); // 只送不接收
            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Restart(); // 重啟計時器
            while (true)
            {
                //if (CheckChargerReaderResponse()) // 有收到回應
                if (((ChargersReader.ChargerResponseIndex == (int)this.deviceID)
                && (ChargersReader.ChargerResponseData == 3)))
                {
                    byte[]? FAULT_STATUS_BYTES = ChargersReader.ReceivedCANBusMessage; // 取得收到的資料
                    if (FAULT_STATUS_BYTES == null)
                    {
                        isReadError = true;
                        _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetFAULT_STATUS_Sync()-ReceivedCANBusMessage is null");
                        return this.FAULT_STATUS;
                    }
                    FAULT_STATUS_Union FAULT_STATUS = new FAULT_STATUS_Union();
                    FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);
                    return FAULT_STATUS;
                }
                if (_stopwatch.ElapsedMilliseconds > timeoutMilliseconds) // 超時
                {
                    isReadError = true;
                    _logger.LogWarning($"NPB450Controller{this.deviceID}-[Linux]-GetFAULT_STATUS_Sync()-Timeout");
                    Task.Delay(100).Wait();
                    return this.FAULT_STATUS;
                }
                await Task.Delay(10);
            }
        }

        private async Task<FAULT_STATUS_Union> GetFAULT_STATUSAsync()
        {
            byte[] FAULT_STATUS_BYTES = await GetStatusFromDeviceAsync(NPB450Controller.CanbusReadCommand.FAULT_STATUS);
            FAULT_STATUS_Union FAULT_STATUS = new FAULT_STATUS_Union();
            FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);
            return FAULT_STATUS;
        }

        private async Task PollingLoop()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }
            while (_running)
            {
                try
                {
                    byte[] VoltageBytes =  await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.READ_VOUT);
                    ushort Voltage = BitConverter.ToUInt16(new byte[] { VoltageBytes[2], VoltageBytes[3] }.ToArray(), 0);
                    //double Voltagef = (double)Voltage / 100;
                    this.Voltage = (double)Voltage / 100;

                    byte[] CurrentBytes =  await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.READ_IOUT);
                    ushort Current = BitConverter.ToUInt16(new byte[] { CurrentBytes[2], CurrentBytes[3] }.ToArray(), 0);
                    //double currentf = (double)Current / 100;
                    this.Current = (double)Current / 100;

                    byte[] CHG_STATUS_BYTES = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.CHG_STATUS);
                    //NPB1700Controller.CHG_STATUS_Union CHG_STATUS = new CHG_STATUS_Union();
                    this.CHG_STATUS.Data = BitConverter.ToUInt16(new byte[] { CHG_STATUS_BYTES[2], CHG_STATUS_BYTES[3] }.ToArray(), 0);
                    //Console.Write(BitConverter.ToString(CHG_STATUS_BYTES));

                    byte[] FAULT_STATUS_BYTES = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.FAULT_STATUS);
                    this.FAULT_STATUS.Data = BitConverter.ToUInt16(new byte[] { FAULT_STATUS_BYTES[2], FAULT_STATUS_BYTES[3] }.ToArray(), 0);

                    string printmsg =
                        "State = " + _chargingStationStateMachine.GetCurrentStateName() +
                        "\nVoltage = " + this.Voltage +
                        "\nCurrent = " + this.Current +
                        "\nFULLM = " + CHG_STATUS.Bits.FULLM +
                        "\nCCM = " + CHG_STATUS.Bits.CCM +
                        "\nCVM = " + CHG_STATUS.Bits.CVM +
                        "\nFVM = " + CHG_STATUS.Bits.FVM +
                        "\nWAKEUP_STOP = " + CHG_STATUS.Bits.WAKEUP_STOP +
                        "\nNTCER = " + CHG_STATUS.Bits.NTCER +
                        "\nBTNC = " + CHG_STATUS.Bits.BTNC +
                        "\nCCTOF = " + CHG_STATUS.Bits.CCTOF +
                        "\nCVTOF = " + CHG_STATUS.Bits.CVTOF +
                        "\nFVTOF = " + CHG_STATUS.Bits.FVTOF;
                    Console.WriteLine(printmsg);
                    //Console.WriteLine($"CAN Status: {status}");

                    // 根據狀態做動作，例如：
                    //if (this.Voltage >= 10)
                    //{
                    //    if (_chargingStationStateMachine._currentState is IdleState)
                    //        _chargingStationStateMachine.TransitionTo<OccupiedState>();
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Polling Error: {ex.Message}");
                }

                await Task.Delay(500); // 控制輪詢頻率
            }
        }

        public void StopPolling()
        {
            _running = false;
        }

        public void StartCharging()
        {
            startChargingTrigger = true;
        }

        public async void StartChargingAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            ICANBusService canBus = new SocketCANBusService();

            int numberOfDataBytes = 1;
            byte[] data = new byte[2 + numberOfDataBytes];
            byte[] commandType = BitConverter.GetBytes((ushort)CanbusWriteCommand.OPERATION);
            commandType.CopyTo(data, 0);
            data[2] = (byte)0x01;
            //_canBusService.SendCommand(data);
            CanMessage canMessage = new CanMessage()
            {
                Id = CanId.FromRaw(deviceCanID),
                Data = data
            };

            await _canBusService.SendAsync(canMessage);
        }

        public void StopCharging()
        {
            stopChargingTrigger = true;
        }

        public async Task<bool> IsCharging()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return false;
            }

            byte[] operation = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.OPERATION);
            Console.WriteLine($"Operation: {operation[0]}");
            if (operation[0] == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Can only change the current when the device is not charging.
        /// </summary>
        /// <param name="targetCurrent"></param>
        /// <returns></returns>
        public bool ChangeChargingCurrent(double targetCurrent)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return false;
            }

            bool changeCurrentSuccess = false;
            try
            {
                //_canBusService.ClearCANBuffer();
                //if (this.IsCharging() == false)
                //    return changeCurrentSuccess;

                ushort curve_cc_value = (ushort)(targetCurrent * 100);
                int numberOfDataBytes = 2;
                byte[] send = new byte[2 + numberOfDataBytes];
                byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusWriteCommand.CURVE_CC);
                sendBytes.CopyTo(send, 0);
                BitConverter.GetBytes(curve_cc_value).CopyTo(send, 2);
                _canBusService.SendCommand(send, deviceCanID);
                changeCurrentSuccess = true;
            }
            catch (Exception ex)
            {
                changeCurrentSuccess = false;
                Console.WriteLine(ex.Message.ToString());
                throw;
            }
            return changeCurrentSuccess;
        }

        private async Task<byte[]> GetStatusFromDeviceAsync(CanbusReadCommand command)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return null;
            }

            uint rpb1700DeviceID = 3;
            uint rpb1700MessageID = 0x000C0100;
            //uint canID = rpb1700DeviceID | rpb1700MessageID; // 確認這是否正確格式
            uint canID = 0x000C0103 | 0x80000000;
            var canId = new CanId { IsExtended = false, Value = canID };
            var data = new byte[8];
            BitConverter.GetBytes((ushort)command).CopyTo(data, 0);

            var message = new CanMessage
            {
                Id = canId,
                Data = data
            };

            // Send command
            byte[] sendBytes = BitConverter.GetBytes((ushort)command);
            _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetStatusFromDevice2()-Command:{command.ToString()} Bytes:{BitConverter.ToString(sendBytes)}");
            _canBusService.SendCommand(sendBytes, deviceCanID);
            //await _canBusService.SendAsync(message);
            // Wait for response
            await Task.Delay(50);

            var response = await _canBusService.ReceiveAsync(100);
            if (response != null)
            {
                Console.WriteLine($"Recv: ID={response.Id}, Data={BitConverter.ToString(response.Data)}");
                Console.WriteLine($"{command} = {Convert.ToHexString(response.Data)}");
                return response.Data;
            }
            else
            {
                Console.WriteLine("Timeout.");
                return Array.Empty<byte>();
            }
        }


        private async Task<byte[]> GetStatusFromDevice2(CanbusReadCommand command)
        {

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return null;
            }
            byte[] sendBytes = BitConverter.GetBytes((ushort)command);
            _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetStatusFromDevice2()-Command:{command.ToString()} Bytes:{BitConverter.ToString(sendBytes)}");
            _canBusService.SendCommand(sendBytes, deviceCanID);

            Thread.Sleep(50);
            //await Task.Delay(50);

            var response = _canBusService.ReceiveMessage();
            return response;
        }

        private async Task GetStatusFromDevice_OnlySend(CanbusReadCommand command)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

                byte[] sendBytes = BitConverter.GetBytes((ushort)command);
                _logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetStatusFromDevice_OnlySend()-Command:{command.ToString()} Bytes:{BitConverter.ToString(sendBytes)}");
                _canBusService.SendCommand(sendBytes, deviceCanID);
            }
        }

        public void TEST()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            Console.WriteLine("Test");
            byte[] sendBytes = BitConverter.GetBytes((ushort)CanbusReadCommand.READ_TEMPERATURE_1);
            _canBusService.SendCommand(sendBytes, deviceCanID);
        }

        private bool CheckChargerReaderResponse()
        { 
            return ((ChargersReader.ChargerResponseIndex == ChargersReader.ChargerIndex) 
                && (ChargersReader.ChargerResponseData == ChargersReader.ChargerCommandData));
        }


    }
}
