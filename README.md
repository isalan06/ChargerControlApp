# ChargerControlApp
Battery Swapping Station ASP.Net 8.0 MVC架構

---
# 目錄

- [Hardware Description](#hardware-description)
  - [MOXA IPC](#moxa-ipc) 
  - [東方馬達驅動器更新](#東方馬達驅動器設定)
- [軟體環境](#軟體環境)
  - [檔案內容說明](#檔案內容說明)
  - [套件](#套件)
- [狀態說明](#狀態說明)
  - [SlotState狀態列舉](#SlotState狀態列舉)
  - [SlotChargeState狀態列舉](#SlotChargeState狀態列舉)
  - [ChargingState狀態列舉](#ChargingState狀態列舉)
  - [手動模式](#手動模式)
  - [上位系統連線狀態](#上位系統連線狀態)
- [參數說明](#參數說明)
  - [系統設定檔](#系統設定檔)
  - [馬達點位資訊檔](#馬達點位資訊檔)
  - [Slot資訊資訊檔](#Slot資訊資訊檔)

設計文件：
- [Design](Design.md)
- [Flow Chart](FlowChart.md)
- [Memo-開發過程隨記](Memo.md)


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
UART_PORT="/dev/ttyM0"

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
  - ~~目前尚未設定到自動開啟~~2025年12月已完成
  - SSH 登入後，執行 cd program/testapp2/publish
  - 執行 ./ChargerControlApp

- 設定開機執行
  - 創建服務文件：在 /etc/systemd/system/ 目錄下創建一個名為 chargercontrolapp.service 的文件。
    ```bash
    sudo nano /etc/systemd/system/chargercontrolapp.service
    ```
  - 編輯服務內容
  ```bash
  [Unit]
  Description=Battery Swapping Station - ASP.NET Core App
  After=network.target setup-serial-can.service
  Requires=setup-serial-can.service

  [Service]
  WorkingDirectory=/home/moxa/program/app/publish/
  ExecStart=/home/moxa/.dotnet/dotnet /home/moxa/program/app/publish/ChargerControlApp.dll
  Restart=always
  # RestartSec=10 # 錯誤發生後 10 秒重啟
  #User=pi # 執行應用程式的 Linux 使用者
  Environment=ASPNETCORE_ENVIRONMENT=Production
  # Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false # 關閉遙測訊息

  [Install]
  WantedBy=multi-user.target
  ```
  - 重新載入 Systemd
  ```bash
  sudo systemctl daemon-reload
  ```
  - 啟用並啟動服務
  ```bash
  sudo systemctl enable chargercontrolapp.service # 開機自動啟動
  sudo systemctl start chargercontrolapp.service # 立即啟動
  ```
  - 檢查狀態
  ```bash
  sudo systemctl status chargercontrolapp.service
  ```

##  東方馬達驅動器設定
- 資料
  - 檔案存放在 OrientalMotorDriver 資料夾中
  - Axis_0_parameter_20251203.mxex: 旋轉軸參數檔
  - Axis_1_parameter_20251203.mxex: Y軸參數檔
  - Axis_2_parameter_20251203.mxex: Z軸參數檔
- 更新東方馬達驅動器(每一軸都要更新參數檔)
  - 安裝 MEXE02 Ver. 4 軟體
  - 使用 USB連接線(USB A to mini USB) 連接電腦與驅動器
  - 開啟 MEXE02 Ver. 4 軟體
  - 開啟參數檔 (選擇檔案)
  - 選擇 連接口(若沒有需確認是否有連接到驅動器或有無安裝驅動程式)
  - 資料寫入


---



# 軟體環境

## 檔案內容說明
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
 ┃ ┣ 📜SystemController.cs
 ┃ ┣ 📜TestController.cs
 ┃ ┗ 📜UnitsController.cs
 ┣ 📂DataAccess                                     # 控制/通訊/模組
 ┃ ┣ 📂CANBus                                       # CANBUS 資料區
 ┃ ┃ ┣ 📂Interfaces                                 # CANBUS 介面區
 ┃ ┃ ┃ ┗ 📜ICANBusService.cs                        
 ┃ ┃ ┣ 📂Linux
 ┃ ┃ ┃ ┗ 📜SocketCANBusService.cs                   # CANBUS 基本服務
 ┃ ┃ ┣ 📂Mocks
 ┃ ┃ ┃ ┗ 📜MockCANBusService.cs
 ┃ ┃ ┣ 📂Models
 ┃ ┃ ┃ ┣ 📜CanId.cs
 ┃ ┃ ┃ ┣ 📜CanMessage.cs
 ┃ ┃ ┃ ┗ 📜CanRouteCommandFrame.cs                  # MW NPB450 循環讀取命令
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
 ┃ ┃ ┃ ┣ 📜SingleMotorPersistence.cs                # 單一馬達參數讀寫模組
 ┃ ┃ ┃ ┗ 📜SingleMotorService.cs                    # 單一馬達資訊讀取和基本動作命令 如 JOG/HOME/MOVE等等
 ┃ ┣ 📂Robot                                        # 三個馬達組合成一個Robot單元，組合動作在RobotController中，此資料夾主要負責Robot的動作程序
 ┃ ┃ ┣ 📂Models                                     # Robot 資料區 - 針對程序
 ┃ ┃ ┃ ┣ 📜DefaultPlaceCarBatteryProcedure.cs       # 放置電池到車輛上的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultPlaceSlotBatteryProcedure.cs      # 放置電池到槽位上的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultProcedure.cs                      # 預設程序的父類別
 ┃ ┃ ┃ ┣ 📜DefaultRotateProcedure.cs                # 旋轉動作的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultTakeCarBatteryProcedure.cs        # 從車輛取出電池的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultTakeSlotBatteryProcedure.cs       # 從槽位取出電池的預設程序
 ┃ ┃ ┃ ┣ 📜DefaultTest1Procedure.cs                 # Slot1~Slot4依序交換測試的預設程序
 ┃ ┃ ┃ ┣ 📜PosErrorFrame.cs                         # 紀錄程序錯誤的格式
 ┃ ┃ ┃ ┣ 📜PosFrame.cs                              # 點位動作的格式
 ┃ ┃ ┃ ┣ 📜ProcedureFrame.cs                        # 流程動作的母類別-PosFrame跟SensorFrame都繼承該類別
 ┃ ┃ ┃ ┣ 📜SensorFrame.cs                           # 感測器檢查的格式
 ┃ ┃ ┃ ┗ 📜TestSlotFrame.cs                         # 測試流程中Slot交換資訊的格式
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
 ┃ ┣ 📜ChargersReader.cs                            # 負責CANBUS回傳資訊的分析與將資訊寫入NPB450Controller
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
 ┣ 📂Parameters                                     # 系統使用的資訊檔範例
 ┃ ┣ 📜MotorPersistence_0.json                      # 軸#0-旋轉軸 點位資訊檔
 ┃ ┣ 📜MotorPersistence_1.json                      # 軸#1-Y軸 點位資訊檔
 ┃ ┣ 📜MotorPersistence_2.json                      # 軸#2-Z軸 點位資訊檔
 ┃ ┗ 📜slot_states.json                             # Slot 狀態及電池記憶資訊檔
 ┣ 📂Properties
 ┃ ┗ 📜launchSettings.json
 ┣ 📂Protos                                         # gRPC server 使用的 proto檔
 ┃ ┣ 📜battery_swapping_station.proto               # 換電站的gRPC Server用
 ┃ ┣ 📜charger_action_service.proto                 # 舊的，已不使用
 ┃ ┣ 📜charger_status_service.proto                 # 舊的，已不使用
 ┃ ┣ 📜device_registration_service.proto            # 與上位 gRPC Server連線，進行註冊及刪除動作 
 ┃ ┣ 📜kernel_device_common.proto                   # 舊的，已不使用
 ┃ ┗ 📜kernel_device_status_service.proto           # 舊的，已不使用 
 ┣ 📂Services                                       # 服務
 ┃ ┣ 📜AppServices.cs                               # App應用
 ┃ ┣ 📜BackgroundService.cs                         # canbus 的 pollingr及Slot狀態機的變更
 ┃ ┣ 📜BatterySwappingStationService.cs             # gRPC Server的服務內容
 ┃ ┣ 📜GrpcChannelManager.cs                        # 舊的，已不使用
 ┃ ┣ 📜GrpcClientService.cs                         # gRPC Client，與上位gRPC Server進行連線並執行註冊及刪除動作
 ┃ ┣ 📜GrpcServiceService.cs                        # 舊的，已不使用 
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
 ┃ ┣ 📂System
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Index.cshtml.cs
 ┃ ┣ 📂Test
 ┃ ┃ ┣ 📜Index.cshtml
 ┃ ┃ ┗ 📜Index.cshtml.cs
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

## 套件
NuGet上所使用的套件
- Grpc.AspNetCore             V2.71.0
- Grpc.Net.Client             V2.71.0
- Grpc.Tools                  V2.72.0
- Smart.Modbus                V1.0.1
- SocketCANSharp              V0.13.0

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
  8. FullCharge # 此Slot上已完成充電並停止充電，同時會等待一段時間後再度充電
  9. SupplyError # MW NPB450產生的錯誤訊號
  10. StateError # 此Slot上的狀態跟電池記憶不同
  11. CommError # 此Slot上發生CANBUS通訊異常

## SlotChargeState狀態列舉
為  gRPC 讀取Slot的狀態列舉
  1. Unspecified # 未知狀態
  2. Empty # 此Slot上無電池
  3. Charging # 此Slot上有電池且在充電中
  4. Floating # 此Slot上有電池且在浮充狀態
  5. Error # 此Slot上發生Error

### SlotState跟SlotChargeState關係
 ```bash
SlotChargeState.Empty       --. SlotState.Initialization
                               ┗ SlotState.Empty
SlotChargeState.Unspecified --. SlotState.NotUsed
                               ┣ SlotState.SupplyError
                               ┣ SlotState.StateError
                               ┗ SlotState.CommError
SlotChargeState.Charging    --.  SlotState.Idle
                               ┣ SlotState.Charging
                               ┣ SlotState.StopCharge
                               ┗ SlotState.FullCharge
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

## 上位系統連線狀態
在 GrpcClientService中有一個參數作為與上位系統(gPRC Server)連線的紀錄: IsOnline <br>

---
# 參數說明

## 系統設定檔
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
    "GRPCRegisterOnlyResponse": true,               // gRPC註冊時，只要收到不回空的回應視為完成
    "CheckBattaryExistByMemory": false,             // 檢查Slot電池存在是否使用記憶，若是false則是利用電壓及電流來判斷
    "CheckBatteryExistValue_Voltage_V": 1.0,        // 確認Slot電池存在的最低電壓，如超過1V視為存在
    "CheckBatteryChargeValue_Voltage_V": 5.0,       // 確認Slot電池充電判斷的最低電壓，如超過5V視為充電中
    "CheckBatteryFullChargeValue_A": 0.1,           // 確認Slot電池已充飽電的最高電流，如低於0.1A視為已充飽電
    "RechargeAfterFullDischarge_Minutes": 10,       // 在充飽電的狀態下，延遲該數據地分鐘後再度充電
    "FullChargeCheckDelay_Seconds": 10              // 判斷充飽電的延遲時間，如在電流低於0.1A的狀態延續10秒轉換成已充飽電的狀態
  }
}
```

## 馬達點位資訊檔

MotorPersistence_x.json; x=0,1,2 => 代表Axis#0(旋轉軸), Axis#1(Y軸), Axis#2(Z軸) <br>
[MotorPersistence_0.json](./ChargerControlApp/Parameters/MotorPersistence_0.json) <br>
[MotorPersistence_1.json](./ChargerControlApp/Parameters/MotorPersistence_1.json) <br>
[MotorPersistence_2.json](./ChargerControlApp/Parameters/MotorPersistence_2.json) <br>
紀錄各軸0~19個位置<br>
格式內容:

```json
[
  {
    "OpType": 1,            // 位置0的運轉模式; 此專案都設為1
    "Position": 13750,      // 位置0的馬達位置; 
    "Velocity": 150         // 位置0的移動速度
  },
  {
    "OpType": 1,            // 位置1的運轉模式; 此專案都設為1
    "Position": -166650,    // 位置1的馬達位置;
    "Velocity": 150         // 位置1的移動速度
  },
  {
    "OpType": 1,            // 位置2的運轉模式; 此專案都設為1
    "Position": 192850,     // 位置2的馬達位置;
    "Velocity": 150         // 位置2的移動速度
  },
  ...以此類推
]
```

## Slot資訊資訊檔

[slot_states.json](./ChargerControlApp/Parameters/slot_states.json) <br>
8個slot的狀態及電池記憶

```json
[
  {
    "Index": 0,               // Slot#1
    "BatteryMemory": false,   // 電池存在記憶
    "State": 7                // SlotState狀態
  },
  {
    "Index": 1,               // Slot#2
    "BatteryMemory": false,   // 電池存在記憶
    "State": 7                // SlotState狀態
  },
  {
    "Index": 2,               // Slot#3
    "BatteryMemory": false,   // 電池存在記憶
    "State": 1                // SlotState狀態
  },
  ...以此類推
]
```
---
