# ChargerControlApp
Battery Swapping Station ASP.Net 8.0 MVC架構

# 目錄

---
# <center> Hardware Description </center>
## MOXA IPC 

- 型號: UC-3434A-T-LTE-WiFi
- 開發使用 LAN2
  - 固定 IP: 192.168.4.127
  - user: moxa
  - password: qwer@1234
- 設定網路
  - 使用MOXA命令: sudo mx-connect-mgmt configure
  - 有設定 LAN2 固定IP 及 Wifi測試用SSID
  - 詳細 請參照 MOXA 手冊
  - 查看可用 Wi-Fi: nmcli device wifi list
  - 連線到 Wi-Fi: nmcli device wifi connect <SSID> password <WIFI_PASSWORD>
  - 驗證是否連線成功: nmcli dev status
- 設定串列通訊
  - 使用MOXA命令設定 RS485兩線式: sudo mx-interface-mgmt serialport P1 set_interface RS-485-2W
  - 設定 RS485 230400,E,8,1: sudo stty -F /dev/ttyM0 230400 cs8 parenb -parodd -cstopb
- 傳遞方式 (暫時使用)
  - 使用 cmd
  - 下達命令scp: scp -prq "c:\users\user\dropbox\alan\case\20250606_seanproject_canbus_canopen\program\chargercontrolapp\chargercontrolapp\bin\release\net8.0\publish" moxa@192.168.4.127:/home/moxa/program/testapp2
- CAN 測試
  - CAN硬體的H跟L之間要安裝120歐姆的電阻，不然會有雜訊
  - 一開始要執行: sudo ip link set can0 up type can bitrate 250000


# <center> 檔案內容說明 <center>
```bash
📦ChargerControlApp
 ┣ 📂.config
 ┃ ┗ 📜dotnet-tools.json
 ┣ 📂bin
 ┃ ┗ 📂Debug
 ┃ ┃ ┗ 📂net8.0
 ┣ 📂Controllers                                    # MVC 架構的Controller-網頁執行的程序
 ┃ ┣ 📜ChargerController.cs
 ┃ ┣ 📜GrpcController.cs
 ┃ ┣ 📜HomeController.cs
 ┃ ┣ 📜MotorController.cs
 ┃ ┗ 📜UnitsController.cs
 ┣ 📂DataAccess                                     # 控制/通訊/模組
 ┃ ┣ 📂CANBus                                       # CANBUS 資料區
 ┃ ┃ ┣ 📂Interfaces                                 # CANBUS 介面區
 ┃ ┃ ┃ ┗ 📜ICANBusService.cs                        # 
 ┃ ┃ ┣ 📂Linux
 ┃ ┃ ┃ ┗ 📜SocketCANBusService.cs
 ┃ ┃ ┣ 📂Mocks
 ┃ ┃ ┃ ┗ 📜MockCANBusService.cs
 ┃ ┃ ┣ 📂Models
 ┃ ┃ ┃ ┣ 📜CanId.cs
 ┃ ┃ ┃ ┗ 📜CanMessage.cs
 ┃ ┣ 📂GPIO                                         # GPIO 資料區 - 已不用GPIO，但拿來介接馬達Sensor訊號
 ┃ ┃ ┣ 📂Models                                     # GPIO 模型 - 放置 GPIO 要用的格式
 ┃ ┃ ┃ ┗ 📜GPIOInfo.cs                              # GPIO 使用的資料格式
 ┃ ┃ ┗ 📂Services                                   # GPIO 服務 - 放置 GPIO 的服務
 ┃ ┃ ┃ ┗ 📜GPIOService.cs                           # GPIO 讀取硬體資料 - 目前不使用，只用來介接 馬達驅動器 的Sensors狀態
 ┃ ┣ 📂Modbus                                       # Modbus 資料區 - 用於連接東方馬達驅動器 - 原本要使用 CANOpen，但後來使用 Modbus 已開發差不多就不改回 CANOpen
 ┃ ┃ ┣ 📂Interfaces                                 # Modbus 介面
 ┃ ┃ ┃ ┗ 📜IModbusRTUService.cs                     # Modbus 對外使用的介面    
 ┃ ┃ ┣ 📂Models                                     # Modbus 模型 - 放置 Modbus 所用到的格式
 ┃ ┃ ┃ ┣ 📜ModbusRTUException.cs                    # Modbus 針對 RTU 自製的通訊錯誤處理格式
 ┃ ┃ ┃ ┣ 📜ModbusRTUFrame.cs                        # Modbus 命令產生及接收回應的格式
 ┃ ┃ ┃ ┗ 📜ModbusRTUServiceException.cs
 ┃ ┃ ┗ 📂Services
 ┃ ┃ ┃ ┗ 📜ModbusRTUService.cs
 ┃ ┣ 📂Motor
 ┃ ┃ ┣ 📂Interfaces
 ┃ ┃ ┃ ┗ 📜ISingleMotorService.cs
 ┃ ┃ ┣ 📂Models
 ┃ ┃ ┃ ┣ 📜MotorAlarmList.cs
 ┃ ┃ ┃ ┣ 📜MotorCommandList.cs
 ┃ ┃ ┃ ┣ 📜MotorFrame.cs
 ┃ ┃ ┃ ┣ 📜MotorId.cs
 ┃ ┃ ┃ ┗ 📜MotorInfo.cs
 ┃ ┃ ┗ 📂Services
 ┃ ┃ ┃ ┗ 📜SingleMotorService.cs
 ┃ ┣ 📂Robot
 ┃ ┃ ┣ 📂Models
 ┃ ┃ ┃ ┣ 📜DefaultPlaceCarBatteryProcedure.cs
 ┃ ┃ ┃ ┣ 📜DefaultPlaceSlotBatteryProcedure.cs
 ┃ ┃ ┃ ┣ 📜DefaultRotateProcedure.cs
 ┃ ┃ ┃ ┣ 📜DefaultTakeCarBatteryProcedure.cs
 ┃ ┃ ┃ ┣ 📜DefaultTakeSlotBatteryProcedure.cs
 ┃ ┃ ┃ ┣ 📜PosErrorFrame.cs
 ┃ ┃ ┃ ┣ 📜PosFrame.cs
 ┃ ┃ ┃ ┣ 📜ProcedureFrame.cs
 ┃ ┃ ┃ ┗ 📜SensorFrame.cs
 ┃ ┃ ┗ 📂Services
 ┃ ┃ ┃ ┗ 📜RobotService.cs
 ┃ ┗ 📂Slot
 ┃ ┃ ┣ 📂Models
 ┃ ┃ ┃ ┣ 📜SlotInfo.cs
 ┃ ┃ ┃ ┗ 📜SlotStateMachineDto.cs
 ┃ ┃ ┗ 📂Services
 ┃ ┃ ┃ ┣ 📜SlotServices.cs
 ┃ ┃ ┃ ┣ 📜SlotStateMachine.cs
 ┃ ┃ ┃ ┗ 📜SlotStatePersistence.cs
 ┣ 📂Hardware
 ┃ ┣ 📜HardwareManager.cs
 ┃ ┣ 📜NPB450Controller.cs
 ┃ ┗ 📜RobotController.cs
 ┣ 📂Models
 ┃ ┣ 📂Motor
 ┃ ┃ ┣ 📜JogHomeParamBatchUpdateDto.cs
 ┃ ┃ ┣ 📜JogHomeParamUpdateDto.cs
 ┃ ┃ ┣ 📜PosVelUpdateDto.cs
 ┃ ┃ ┣ 📜RotateProcedureRequest.cs
 ┃ ┃ ┣ 📜SavePosVelDto.cs
 ┃ ┃ ┣ 📜SetPositionRequest.cs
 ┃ ┃ ┗ 📜SlotRequest.cs
 ┃ ┣ 📜ErrorViewModel.cs
 ┃ ┗ 📜JogHomeParam.cs
 ┣ 📂Properties
 ┃ ┗ 📜launchSettings.json
 ┣ 📂Protos
 ┃ ┣ 📜battery_swapping_station.proto
 ┃ ┣ 📜charger_action_service.proto
 ┃ ┣ 📜charger_status_service.proto
 ┃ ┣ 📜kernel_device_common.proto
 ┃ ┣ 📜kernel_device_registration_service.proto
 ┃ ┗ 📜kernel_device_status_service.proto
 ┣ 📂Services
 ┃ ┣ 📜AppServices.cs
 ┃ ┣ 📜BackgroundService.cs
 ┃ ┣ 📜BatterySwappingStationService.cs
 ┃ ┣ 📜GrpcChannelManager.cs
 ┃ ┣ 📜GrpcClientService.cs
 ┃ ┣ 📜GrpcServiceService.cs
 ┃ ┣ 📜MonitoringService.cs
 ┃ ┣ 📜ServiceRegistrationExtensions.cs
 ┃ ┗ 📜StateMachine.cs
 ┣ 📂Test
 ┃ ┣ 📂Modbus
 ┃ ┃ ┣ 📜MyModbusTesting.cs
 ┃ ┃ ┗ 📜NModbusTesting.cs
 ┃ ┗ 📂Robot
 ┃ ┃ ┗ 📜RobotTestProcedure.cs
 ┣ 📂Utilities
 ┃ ┣ 📜AppSettings.cs
 ┃ ┗ 📜ConfigLoader.cs
 ┣ 📂Views
 ┃ ┣ 📂Charger
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Index.cshtml.cs
 ┃ ┣ 📂Grpc
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Index.cshtml.cs
 ┃ ┣ 📂Home
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Privacy.cshtml
 ┃ ┣ 📂Motor
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┣ 📜index.cshtml.cs
 ┃ ┃ ┗ 📜JogHome.cshtml
 ┃ ┣ 📂Shared
 ┃ ┃ ┣ 📜Error.cshtml
 ┃ ┃ ┣ 📜_Layout.cshtml
 ┃ ┃ ┣ 📜_Layout.cshtml.css
 ┃ ┃ ┗ 📜_ValidationScriptsPartial.cshtml
 ┃ ┣ 📂Units
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Index.cshtml.cs
 ┃ ┣ 📜_ViewImports.cshtml
 ┃ ┗ 📜_ViewStart.cshtml
 ┣ 📂wwwroot
 ┃ ┣ 📂css
 ┃ ┃ ┗ 📜site.css
 ┃ ┣ 📂js
 ┃ ┃ ┗ 📜site.js
 ┃ ┗ 📜favicon.ico
 ┣ 📜appsettings.Development.json
 ┣ 📜appsettings.json
 ┣ 📜ChargerControlApp.csproj
 ┗ 📜Program.cs
```
---