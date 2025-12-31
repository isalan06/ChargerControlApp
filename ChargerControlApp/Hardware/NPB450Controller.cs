using ChargerControlApp.DataAccess;
using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Linux;
using ChargerControlApp.DataAccess.CANBus.Models;
using ChargerControlApp.Services;
using ChargerControlApp.Utilities;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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


namespace ChargerControlApp.Hardware
{
    public class NPB450Controller
    {
        public static int NPB450ControllerInstnaceMaxNumber = 8; // TODO: 之後改成設定檔
        public static bool ChargerUseAsync = false; // TODO: 之後改成設定檔
        private readonly ILogger<NPB450Controller> _logger; 

        private readonly ICANBusService _canBusService;
        private readonly ChargingStationStateMachine _chargingStationStateMachine;
        private readonly AppSettings _appSettings;

        private static uint rpb1700DeviceID = 3; // 此參數為固定測值，後續不使用該數值
        private static uint rpb1700MessageID = 0x000C0100; 
        private readonly uint canID = rpb1700DeviceID | rpb1700MessageID; // 此參數為固定測值，後續不使用該數值
        //充電器對控制器的MessageID=0x000C00XX
        //控制器對充電器的MessageID=0x000C01XX
        //控制器對充電氣廣播的MessageID=0x000C01FF
        public uint deviceID { get; private set; } = 0; // 讓每一台NPB450有不同的ID
        public uint deviceCanID { get { return (deviceID | rpb1700MessageID); } } // 讓每一台NPB450有不同的CANID

        public double Voltage = 0;
        public double Current = 0;
        public FAULT_STATUS_Union FAULT_STATUS;
        public CHG_STATUS_Union CHG_STATUS;
        public bool IsUsed { get; set; } = false;
        private bool startChargingTrigger = false;
        private bool stopChargingTrigger = false;
        public bool FinalStartChargingTrigger { get; internal set; } = false;
        public bool FinalStopChargingTrigger { get; internal set; } = false;

        // timeout
        private bool isReadError = false;
        private long timeoutMilliseconds = 1000;
        private Stopwatch stopwatch = new Stopwatch(); 
        private Stopwatch rechargeTimer = new Stopwatch();
        private Stopwatch fullchargeCheckDelay = new Stopwatch();

        public CanRouteCommandFrameList RoutueCommandFrames { get; set; } = new CanRouteCommandFrameList();
        public bool IsReadTimeout
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return false; // Windows 模擬環境直接回傳 false
                }
                return RoutueCommandFrames.IsReadTimeout;
            }
        }
        public bool IsCompletedOneTime
        {
            get
            {
                if(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return true; // Windows 模擬環境直接回傳 true
                }
                return RoutueCommandFrames.IsCompletedOneTime;
            }
        }
        public ulong CycleCount
        {
            get
            {
                return RoutueCommandFrames.CycleCount;
            }
        }
        public bool IsTriggerStartCharging { get; internal set; } = false;
        public bool IsFullChargingTrigger { get; set; } = false;

        public bool IsSupplyError
        {
            get
            {
                bool result = false;

                result =    FAULT_STATUS.Bits.HI_TEMP ||
                            FAULT_STATUS.Bits.OLP ||
                            FAULT_STATUS.Bits.OTP ||
                            FAULT_STATUS.Bits.OVP ||
                            FAULT_STATUS.Bits.SHORT ||
                            FAULT_STATUS.Bits.AC_FAIL;

                return result;
            }
        }

        public bool IsBatteryExist
        {
            get
            {
                bool result = false;

                if (IsCompletedOneTime)
                {
                    if (!IsReadTimeout)
                    {
                        if (Voltage > _appSettings.CheckBatteryExistValue_Voltage_V)
                            result = true;
                    }
                }

                return result;
            }
        }

        public bool IsCharging
        {
            get
            {
                bool result = false;
                if (IsCompletedOneTime)
                {
                    if (!IsReadTimeout)
                    {
                        if (Voltage > _appSettings.CheckBatteryChargeValue_Voltage_V)
                            result = true;
                    }
                }
                return result;
            }
        }

        public bool IsFullCharged
        {
            get
            {
                bool result = false;
                if (IsCompletedOneTime)
                {
                    if (!IsReadTimeout)
                    {
                        if (IsBatteryExist)
                        {
                            bool flag = IsCharging && (Current < _appSettings.CheckBatteryFullChargeValue_A);
                            if (!flag)
                            {
                                if (fullchargeCheckDelay.IsRunning)
                                {
                                    RecalculateFullChargedStatus();
                                }
                            }
                            else if (fullchargeCheckDelay.ElapsedMilliseconds > _appSettings.FullChargeCheckDelay_Seconds * 1000)
                                result = true;
                            if (flag)
                            {
                                if (!fullchargeCheckDelay.IsRunning) { fullchargeCheckDelay.Start(); }
                            }
                        }
                        else
                        {
                            if (fullchargeCheckDelay.IsRunning)
                            {
                                RecalculateFullChargedStatus();
                            }
                        }
                    }
                }
                return result;
            }
        }

        public double SOC_Percentage
        {
            get
            {
                double result = 0.0;
                if (IsCompletedOneTime)
                {
                    if (!IsReadTimeout)
                    {
                        if (IsFullChargingTrigger) result = 100.0;
                        else if (this.Voltage >= 58.4) result = 100.0;
                        else if (this.Voltage < 47.2) result = 0.0;
                        else
                        {
                            result = 0.0241 * this.Voltage * this.Voltage - 1.4505 * this.Voltage + 60.214;
                        }
                    }
                }
                return result;
            }
        }
        public void RecalculateFullChargedStatus()
        {
                fullchargeCheckDelay.Stop();
                fullchargeCheckDelay.Reset();
        }


        public void ResetRechargeTimer()
        {
            rechargeTimer.Restart();
        }
        public bool IsRechargeTimeout()
        {
            if (rechargeTimer.ElapsedMilliseconds >= _appSettings.RechargeAfterFullDischarge_Minutes * 1000 * 60)
                return true;
            else
                return false;
        }

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

        public NPB450Controller(ChargingStationStateMachine stateMachine, ICANBusService canBusService, int id, ILogger<NPB450Controller> logger, AppSettings appSettings)
        {
            _appSettings = appSettings;
            _chargingStationStateMachine = stateMachine;
            _canBusService = canBusService;
            deviceID = (uint)id;
            _logger = logger;
            //Task.Run(() => PollingLoop());
        }

        /// <summary>
        /// 主要的輪詢迴圈，會持續呼叫 PollingOnce 方法來取得充電器的狀態。
        /// </summary>
        public async void PollingOnce()
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-Start");
                _canBusService.ClearCANBuffer();
                // 這裡是實際和硬體通訊的地方
                var commandFrame = new CanRouteCommandFrame();
                var isFinal = false;
                bool routeResult = RoutueCommandFrames.Next(out commandFrame, out isFinal);
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-Next Result: {routeResult}, IsFinal: {isFinal}");
                if (routeResult)
                {
                    await GetStatusFromDevice_OnlySend(commandFrame.Command);
                }

                // 需要完整的輪詢週期才進行啟動/停止充電的指令
                if (RoutueCommandFrames.IsCompletedOneTime)
                {
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
                        IsTriggerStartCharging = true;
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
                        IsTriggerStartCharging = false;
                    }
                }
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-PollingOnce()-End");
            }
            else
            {
                // Windows 模擬資料
                _Windows_PollingOnce();
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
                // Windows 模擬資料
                _Windows_PollingOnce();
            }
        }

        private void _Windows_PollingOnce()
        {
            // 模擬資料
            if (this.IsUsed)
            {
                this.Voltage += 1.0 + 0.005 * (double)deviceID;
                if(deviceID != 3) this.Current += 0.015 + 0.001 * (double)deviceID;
                if (this.Voltage >= 60.0) this.Voltage = 47.0;
                if(this.Voltage <= 47.0) this.Voltage = 47.0;
                if (this.Current >= 5.0) this.Current = 0.0;

                if (deviceID == 2)
                {
                    this.Voltage = 0.8;
                }
                else if (deviceID == 3)
                {
                    this.Current += 0.001;
                    if (this.Current >= 0.15)
                    {
                        this.Current = 0.0;
                    }
                }
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

        public double GetCachedVoltage() { return this.Voltage; }
        public double GetCachedCurrent() { return this.Current; }
        public CHG_STATUS_Union GetCachedCHG_STATUS() { return this.CHG_STATUS; }
        public FAULT_STATUS_Union GetCachedFAULT_STATUS() { return this.FAULT_STATUS; }

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


        public void StopPolling()
        {
            _running = false;
        }

        public void StartCharging()
        {
            startChargingTrigger = true;
            stopChargingTrigger = false;

            FinalStartChargingTrigger = true;
            FinalStopChargingTrigger = false;
        }

        public void StopCharging()
        {
            stopChargingTrigger = true;
            startChargingTrigger = false;

            FinalStopChargingTrigger = true;
            FinalStartChargingTrigger = false;
        }

        //public async Task<bool> IsCharging()
        //{
        //    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        return false;
        //    }

        //    byte[] operation = await GetStatusFromDevice2(NPB450Controller.CanbusReadCommand.OPERATION);
        //    Console.WriteLine($"Operation: {operation[0]}");
        //    if (operation[0] == 0)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

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
                //_logger.LogInformation($"NPB450Controller{this.deviceID}-[Linux]-GetStatusFromDevice_OnlySend()-Command:{command.ToString()} Bytes:{BitConverter.ToString(sendBytes)}");
                _canBusService.SendCommand(sendBytes, deviceCanID);
            }
        }


    }
}
