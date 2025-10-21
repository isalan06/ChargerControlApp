# ChargerControlApp
Battery Swapping Station ASP.Net 8.0 MVC架構

---
# 目錄

- [Hardware Description](#hardware-description)
  - [MOXA IPC](#moxa-ipc) 
- [檔案內容說明](#檔案內容說明)
- [狀態說明](#狀態說明)
- [參數說明](#參數說明)

設計文件：
- [Flow Chart](FlowChart.md)


---
# Hardware Description
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
- 硬體設定
  - 建立設定檔，已設定Serial跟Can: 
    nano ~/setup_serial_can.sh
  - 設定檔內容
```bash
#!/bin/bash
# 設定 UART Serial Port 與 CANBus 參數

# UART Port
UART_PORT="/dev/ttyS1"

# CAN Port
CAN_PORT="can0"
CAN_BITRATE=250000

echo "=== 設定 UART (${UART_PORT}) 為 230400,E,8,1 ==="
if [ -e "$UART_PORT" ]; then
    stty -F $UART_PORT 230400 cs8 parenb -parodd -cstopb
    stty -F $UART_PORT -a
else
    echo "找不到 $UART_PORT，請確認是否存在"
fi

echo "=== 啟用 CAN (${CAN_PORT})，bitrate=${CAN_BITRATE} ==="
if ip link show $CAN_PORT > /dev/null 2>&1; then
    sudo ip link set $CAN_PORT down 2>/dev/null
    sudo ip link set $CAN_PORT up type can bitrate $CAN_BITRATE
    ip -details link show $CAN_PORT | grep -A5 can
else
    echo "找不到 $CAN_PORT，請確認驅動與硬體"
fi
```
-
  - 設定執行權限: chmod +x ~/setup_serial_can.sh
  - 執行測試: ./setup_serial_can.sh
- 開機設定
  - 建立服務檔: sudo nano /etc/systemd/system/setup-serial-can.service
  - 寫入:
  ```bash
  [Unit]
  Description=Setup UART and CAN on boot
  After=network.target

  [Service]
  Type=oneshot
  ExecStart=/home/moxa/setup_serial_can.sh

  [Install]
  WantedBy=multi-user.target

  ```
  - 啟用: sudo systemctl enable setup-serial-can.service
- 執行程式碼
  - 目前尚未設定到自動開啟
  - SSH 登入後，執行 cd program/testapp2/publish
  - 執行 ./ChargerControlApp


---
# 檔案內容說明
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
 ┃ ┃ ┃ ┗ 📜ModbusRTUServiceException.cs             # Modbus 服務錯誤處理格式
 ┃ ┃ ┗ 📂Services                                   # Modbus 服務 - 放置 Modbus 的服務
 ┃ ┃ ┃ ┗ 📜ModbusRTUService.cs                      # 讀取/寫入 Modbus RTU 通訊位置 含 Serial Port元件
 ┃ ┣ 📂Motor                                        # Motor 資料區 - 針對單一軸馬達
 ┃ ┃ ┣ 📂Interfaces                                 # Motor 介面
 ┃ ┃ ┃ ┗ 📜ISingleMotorService.cs                   # Motor 單一馬達對外界面
 ┃ ┃ ┣ 📂Models                                     # Motor 模型 - 放置 Motor 所用到的格式
 ┃ ┃ ┃ ┣ 📜MotorAlarmList.cs                        # 東方馬達 BLDC 所對應的錯誤碼及其意思
 ┃ ┃ ┃ ┣ 📜MotorCommandList.cs                      # Motor 所使用到寫入或讀取 BLDC資訊的列表
 ┃ ┃ ┃ ┣ 📜MotorFrame.cs                            # Motor 下達 讀取或寫入 的命令及內容
 ┃ ┃ ┃ ┣ 📜MotorId.cs                               # Motor ID 及 Slave Address
 ┃ ┃ ┃ ┗ 📜MotorInfo.cs                             # 馬達資訊儲存格式
 ┃ ┃ ┗ 📂Services                                   # Motor 服務
 ┃ ┃ ┃ ┗ 📜SingleMotorService.cs                    # 單一馬達資訊讀取和基本動作命令 如 JOG/HOME/MOVE等等
 ┃ ┣ 📂Robot                                        # 三個馬達組合成一個Robot單元，組合動作在RobotController中，此資料夾主要負責Robot的動作程序
 ┃ ┃ ┣ 📂Models                                     # Robot 資料區 - 針對程序
 ┃ ┃ ┃ ┣ 📜DefaultPlaceCarBatteryProcedure.cs       # 放置電池到車輛上的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultPlaceSlotBatteryProcedure.cs      # 放置電池到槽位上的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultRotateProcedure.cs                # 旋轉動作的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultTakeCarBatteryProcedure.cs        # 從車輛取出電池的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultTakeSlotBatteryProcedure.cs       # 從槽位取出電池的預設程序
 ┃ ┃ ┃ ┣ 📜PosErrorFrame.cs                         # 紀錄程序錯誤的格式
 ┃ ┃ ┃ ┣ 📜PosFrame.cs                              # 點位動作的格式
 ┃ ┃ ┃ ┣ 📜ProcedureFrame.cs                        # 流程動作的母類別-PosFrame跟SensorFrame都繼承該類別
 ┃ ┃ ┃ ┗ 📜SensorFrame.cs                           # 感測器檢查的格式
 ┃ ┃ ┗ 📂Services                                   # Robot服務
 ┃ ┃ ┃ ┗ 📜RobotService.cs                          # Robot的半自動流程及全自動流程
 ┃ ┗ 📂Slot                                         # 槽位資料區 - 建立虛擬槽位資訊，資料交換及狀態管理
 ┃ ┃ ┣ 📂Models                                     # 槽位模型
 ┃ ┃ ┃ ┣ 📜SlotInfo.cs                              # 槽位的資料格式
 ┃ ┃ ┃ ┗ 📜SlotStateMachineDto.cs                   # 槽位的狀態格式
 ┃ ┃ ┗ 📂Services                                   # 槽位服務
 ┃ ┃ ┃ ┣ 📜SlotServices.cs                          # 槽位狀態變化及資訊儲存
 ┃ ┃ ┃ ┣ 📜SlotStateMachine.cs                      # 槽位狀態機，以更換狀態為主
 ┃ ┃ ┃ ┗ 📜SlotStatePersistence.cs                  # 狀態及電池記憶讀取儲存功能
 ┣ 📂Hardware                                       # 硬體資料區 - 以硬體為主的控制器
 ┃ ┣ 📜HardwareManager.cs                           # 管理所有硬體
 ┃ ┣ 📜NPB450Controller.cs                          # 單一台NPB450 資訊讀取及動作
 ┃ ┗ 📜RobotController.cs                           # Robot 組合的動作及流程
 ┣ 📂Models                                         # MVC 架構的Model-網頁執行的格式
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
 ┣ 📂Protos                                         # gRPC server 使用的 proto檔
 ┃ ┣ 📜battery_swapping_station.proto               # 換電站的gRPC Server用
 ┃ ┣ 📜charger_action_service.proto                 # 舊的，可能需要更換
 ┃ ┣ 📜charger_status_service.proto                 # 舊的，可能需要更換
 ┃ ┣ 📜kernel_device_common.proto                   # 舊的，可能需要更換
 ┃ ┣ 📜kernel_device_registration_service.proto     # 舊的，可能需要更換 
 ┃ ┗ 📜kernel_device_status_service.proto           # 舊的，可能需要更換 
 ┣ 📂Services                                       # 服務
 ┃ ┣ 📜AppServices.cs                               # App應用
 ┃ ┣ 📜BackgroundService.cs                         # canbus 的 pollingr及Slot狀態機的變更
 ┃ ┣ 📜BatterySwappingStationService.cs             # gRPC Server的服務內容
 ┃ ┣ 📜GrpcChannelManager.cs                        # 舊的，可能需要更換 
 ┃ ┣ 📜GrpcClientService.cs                         # 舊的，可能需要更換 
 ┃ ┣ 📜GrpcServiceService.cs                        # 舊的，可能需要更換 
 ┃ ┣ 📜MonitoringService.cs                         # 管理設備狀態機轉換及背景處理
 ┃ ┣ 📜ServiceRegistrationExtensions.cs             # 註冊 DI
 ┃ ┗ 📜StateMachine.cs                              # 設備狀態機，在變更狀態時可進行處理
 ┣ 📂Test                                           # 測試用
 ┃ ┣ 📂Modbus
 ┃ ┃ ┣ 📜MyModbusTesting.cs
 ┃ ┃ ┗ 📜NModbusTesting.cs
 ┃ ┗ 📂Robot
 ┃ ┃ ┗ 📜RobotTestProcedure.cs
 ┣ 📂Utilities                                      # 元件區
 ┃ ┣ 📜AppSettings.cs                               # 參數設定
 ┃ ┗ 📜ConfigLoader.cs                              # 設定檔載入
 ┣ 📂Views                                          # MVC 架構的View-網頁UI的部分
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
 ┣ 📂wwwroot                                        # 網頁資源區
 ┃ ┣ 📂css
 ┃ ┃ ┗ 📜site.css
 ┃ ┣ 📂js
 ┃ ┃ ┗ 📜site.js
 ┃ ┗ 📜favicon.ico
 ┣ 📜appsettings.Development.json
 ┣ 📜appsettings.json                               # 參數檔
 ┣ 📜ChargerControlApp.csproj
 ┗ 📜Program.cs                                     # 主程式
```
---
# 狀態說明
## SlotState狀態列舉
為SlotService判別Slot的目前狀態
  1. Initialization # 初始化
  2. NotUsed # 此Slot未使用
  3. Empty # 此Slot上無電池
  4. Idle # 此Slot上有電池但未充電，可觸發充電命令
  5. Charging # 此Slot上有電池且在充電中
  6. Floating # 此Slot上有電池且在浮充狀態
  7. StopCharge # 此Slot上有電池但下達停止充電以待取出
  8. SupplyError # MW NPB450產生的錯誤訊號
  9. StateError # 此Slot上的狀態跟電池記憶不同

## SlotChargeState狀態列舉
為  gRPC 讀取Slot的狀態列舉
  1. Unspecified # 未知狀態
  2. Empty # 此Slot上無電池
  3. Charging # 此Slot上有電池且在充電中
  4. Floating # 此Slot上有電池且在浮充狀態

### SlotState跟SlotChargeState關係
 ```bash
SlotChargeState.Empty       --. SlotState.Initialization
                               ┗ SlotState.Empty
SlotChargeState.Unspecified --. SlotState.NotUsed
                               ┣ SlotState.SupplyError
                               ┗ SlotState.StateError
SlotChargeState.Charging    --.  SlotState.Idle
                               ┣ SlotState.Charging
                               ┗ SlotState.StopCharge
SlotChargeState.Floating    --.  SlotState.Floating

 ```

## ChargingState狀態列舉
為整機設備的狀態，排除Slot狀態
  1. Unspecified # 未知狀態
  2. Initial # 初始化
  3. Idle # 等待命令
  4. Swapping # 執行交換電池中
  5. Manual # 在手動模式，遠端無法下達命令
  6. Error # 設備有錯誤發生

## 手動模式
在 RobotService中有一個參數作為手動模式的切換: IsManualMode <br>


---
# 參數說明

<h3> 參數格式及說明 </h3>
appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AppSettings": {
    "ServerIp": "http://localhost:50051",
    "ChargingStationName": "StationA",
    "MaxChargingCurrent": 20,
    "CanInterface": "can0",                         // MW NPB450 連接CAN介面接口
    "CanBitrate": 250000,                           // MW NPB450 CANBus通訊速度
    "PortName": "COM1",                             // BLDC Driver RS485 介面接口(Windows)
    "PortNameLinux": "/dev/ttyM0",                  // BLDC Driver RS485 介面接口(Linux)
    "PowerSupplyInstanceNumber": 4,                 // NPB450 實際安裝數量
    "PositionInPosOffset": 3000,                    // 到位檢查位置範圍
    "SensorCheckPass": false,                       // 測試用，在流程動作中不檢查在席感測器
    "ServoOnAndHomeAfterStartup": false             // 在狀態機變成Initial時是否執行Servo On跟原點復歸
    "ChargerUseAsync": true                         // SocketCANBusService 中使用 Async
  }
}
```

---
