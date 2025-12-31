<h1> 開發隨記 </h1>

# 20251231

## 預計要做的事情
1. 確認電池在Slot上的反應
- 在Charging Start和有電池的狀況下, 電壓電流的反應
 - V: 58.620
 - A: 6.040
- 在Charging Start和無電池的狀況下，電壓電流的反應
 - V: 0.03~0.06 (一開始會衝高)
 - A: 0.00
- 在Charging Stop和有電池的狀況下, 電壓電流的反應
 - V: 3.59
 - A: 0
- 在Charging Stop和無電池的狀況下，電壓電流的反應
 - V: 0.03~0.06
 - A: 0.00
- 在Charging Start和有電池但有BTNC訊號，電壓電流的反應
 - V: 3.58
 - A: 0.00

2. 確認 SlotStateMachine 流程，是否要中途檢查

3. 設定開機後開啟軟體

## 刪除舊有服務
- 檢查舊服務
```bash
systemctl status charger-control-app.service
```

- 關閉開機啟動
```bash
sudo systemctl disable charger-control-app.service
```

## 新增服務
- 創建服務
```bash
sudo nano /etc/systemd/system/chargercontrolapp.service
```

- 編輯內容
```bash
[Unit]
Description=Battery Swapping Station - ASP.NET Core App
After=network.target setup-serial-can.service
Requires=setup-serial-can.service

[Service]
WorkingDirectory=/home/moxa/program/app/publish/
ExecStart=/home/moxa/.dotnet/dotnet /home/moxa/program/app/publish/ChargerControlApp.dll
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

# 20251229

## 開啟啟動測試
使用樹梅派來測試

- 創建服務文件：在 /etc/systemd/system/ 目錄下創建一個名為 chargercontrolapp.service 的文件。

```bash
sudo nano /etc/systemd/system/chargercontrolapp.service
```

- 編輯服務內容(有問題，請用下面的)
```bash
[Unit]
Description=Battery Swapping Station - ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=/home/pi/Program/app/publish/
ExecStart=/usr/bin/dotnet /home/pi/Program/app/publish/ChargerControlApp.dll
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

- 出現問題
```bash
chargercontrolapp.service - Battery Swapping Station - ASP.NET Core App
     Loaded: loaded (/etc/systemd/system/chargercontrolapp.service; enabled; preset: enabled)
     Active: failed (Result: exit-code) since Mon 2025-12-29 19:54:26 CST; 3s ago
   Duration: 4ms
    Process: 1753 ExecStart=/usr/bin/dotnet /home/pi/Program/app/publish/ChargerControlApp.dll (code=exited, status=203/EXEC)
   Main PID: 1753 (code=exited, status=203/EXEC)
        CPU: 3ms

Dec 29 19:54:26 BSS-pi4 systemd[1]: chargercontrolapp.service: Scheduled restart job, restart counter is at 5.
Dec 29 19:54:26 BSS-pi4 systemd[1]: Stopped chargercontrolapp.service - Battery Swapping Station - ASP.NET Core App.
Dec 29 19:54:26 BSS-pi4 systemd[1]: chargercontrolapp.service: Start request repeated too quickly.
Dec 29 19:54:26 BSS-pi4 systemd[1]: chargercontrolapp.service: Failed with result 'exit-code'.
Dec 29 19:54:26 BSS-pi4 systemd[1]: Failed to start chargercontrolapp.service - Battery Swapping Station - ASP.NET Core App.
```
錯誤 status=203/EXEC 表示 systemd 無法執行 ExecStart 中指定的命令。通常是因為找不到 dotnet 或路徑不正確。

- 確認 dotnet 的實際路徑
```bash
# 查找 dotnet 的完整路徑
which dotnet

# 或者
whereis dotnet

# 測試 dotnet 是否可以執行
dotnet --version
```
結果為 /home/pi/.dotnet/dotnet

- 修改 檔案並增加等待 setup-serial-can.service完成後才執行

```bash
sudo nano /etc/systemd/system/chargercontrolapp.service
```
內容
```bash
[Unit]
Description=Battery Swapping Station - ASP.NET Core App
After=network.target setup-serial-can.service
Requires=setup-serial-can.service

[Service]
WorkingDirectory=/home/pi/Program/app/publish/
ExecStart=/home/pi/.dotnet/dotnet /home/pi/Program/app/publish/ChargerControlApp.dll
Restart=always
# RestartSec=10 # 錯誤發生後 10 秒重啟
#User=pi # 執行應用程式的 Linux 使用者
Environment=ASPNETCORE_ENVIRONMENT=Production
# Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false # 關閉遙測訊息

[Install]
WantedBy=multi-user.target
```

- 重新載入並啟動服務
```bash
# 重新載入 systemd 設定
sudo systemctl daemon-reload

# 清除失敗狀態
sudo systemctl reset-failed chargercontrolapp.service

# 啟動服務
sudo systemctl start chargercontrolapp.service

# 檢查狀態
sudo systemctl status chargercontrolapp.service
```

# 20251220

## gRPC 調整項目
1. 新增gRPC 註冊判斷參數，只要有回應就會成功
2. 等待 gRPC 註冊回應有success的資料，在進行開發

## 電池相關項目
- 利用 NPB450的電壓電流來判別狀態
  - 1V 以下當作無電池
  - 1V ~ 5V 當作有電池
  - 5V 以上但是 0.1A以下當作已充飽電
    - 新增 FullCharged 來暫停充電
    - 在 Charging 跟 Floating 狀態下，在上面條件成立時，累計一定時間轉換成 FullCharged 狀態
    - 當 FullCharged 狀態下 累計一定時間後重新充電，回到 Charging 狀態
  - 當狀態錯誤後處理方式
- Slot Battery Memory 跟 Charger 電池有無判別
  - 新增參數來切換使用哪一種為主
  - 調整電池有無狀態判斷條件
- Slot狀態條件重新規劃
  - 初始化狀態去判別所有狀態
  - 規劃Slot狀態機流程
  - 錯誤狀態處理

## 其他問體處理
- 有發現 馬達手動頁面的SET按下後，參數沒有更新，但按 Load有回應，在確認看看
- 新增 目前狀態顯示 以便於了解目前狀態

## 開啟啟動測試
- 現狀: 之前的設定有問題，請參照 [service無法執行](#20251029)
- 處理想法: 用Raspberry pi 4 來做測試

# 20251203

## 馬達運作常出現位置無法到位
- 原因解析: 20251205發現是因為設定位置命令下達後收到回應，驅動器並不會馬上反應，當在反應前程式又讀取命令位置時是下命令前的位置，導致驅動器執行馬達動作時會執行上一次位置，但程式會用這次位置來做判斷，導致偏差量過大
- 解決方案: 在下達位置變更命令後，需要等待確認驅動器已經變更位置，才下達啟動命令

## gRPC微幅變更
1. 註冊Device的回應新增Success與否，判斷連線成功也用這個條件，不要用device_name，等新的proto後修改
2. 電池資訊新增Error狀態，已經做初步修改，等待新的proto做確認

## 優化1: Slot狀態
- 起源: 目前電池出現問題難以排除錯誤
- 方向: 新增排除Slot及電池錯誤狀態手段，並優化初始化狀態的功能

## 功能1: 系統開啟時自動開啟程式
- 現狀: 之前的設定有問題，請參照 [service無法執行](#20251029)
- 處理想法: 用Raspberry pi 4 來做測試

## 功能2: CANBUS重置功能
- 修改1: 顯示讀取資訊在介面上，以更容易了解CANBUS運行狀況
- 修改2: 重置流程
- 確認1: Command Flow

# 20251114-20251126

## 優化1: CANBUS寫法
- 起源: 目前用同步的方法，但將Send跟Read分開兩個執行緒，只要Send的命令間隔較長就比較不會有問題，但不能確認後續的環境狀況會不會讓Read執行Cycle Time超過Send的命令間隔，因此看是否有更好的寫法
- 優化想法: 在Send的序列中不用考慮回應，但依然需要有Send的時間間隔，且需要做一個定期掃描哪一個DeviceID一直沒有回應，要減少該ID的發送命令次數

## 優化2: 自動流程動作增加
- 起源: 有時候會需要中斷流程，之後再繼續，另外有發現在Test1流程下狀態機是Idle
- 確認事項: 先確定 主流程跟子流程是否容易增加，及狀態機

## 優化3: 數量擴充
- 現狀: 目前是用8個 Slot， 點位部分=>Theta軸用3個點, Y軸用4個點, Z軸用19個點, 連續運轉各軸各用5個點位
- 調整方向: 未來先以64個Slot的擴充方向，電源供應器也一併調整數量
- 調整1: 連續運轉轉移到由20~24轉移到200~204
- 調整2: 開放0~199設定
- 調整3: 增加點位名稱
- 注意事項: 目前尚無法得知真實的硬體狀態，所以先以擴充數量為主

## 優化4: UI調整
- 現狀: 首頁狀態顯示目前狀態不明確，無法一目了然
- 優化想法: 將目前狀態明確顯示在首頁中，警報顯示用明確的描述

## 優化5: 動作時間縮短
- 現狀: 目前動作間有固定延遲
- 調整作法: 將原本固定延遲寫成參數方式(X)，改由減少原本等待秒數及優化等待程序(O)


## 優化6: Y0 & Z0 常無法到位
- 現狀: 連續動作中，向Y0或Z0移動常會出現無法到位報警
- 確認項目: 先確認 發生原因
- 猜想原因: 可能為讀取馬達狀態為多個命令所組成，但用到的時候可能部分狀態尚未讀取導致判斷錯誤
- 猜想解法: 將同一個馬達狀態讀取先暫存到記憶體，等所有讀取命令均得到回應時進行狀態由暫存區移至實際變數中

## 功能1: 系統開啟時自動開啟程式
- 現狀: 之前的設定有問題，請參照 [service無法執行](#20251029)
- 處理想法: 用Raspberry pi 4 來做測試

# 20251111

## 問題1: 重複進入Home流程
- 發生現狀: 程式一開啟就會進入HOME流程，但目前已修正成 要先連到gRPC Server才會進行HOME流程，一但連線gRPC Server完成或跳過，  HOME流程就會在執行一遍
- 原因: 開啟 HOME 流程有兩個地方，一個在BackgroundServive.cs中(原先的設計)，一個在StateMachine.cs中(後續修改到這個模組執行,但是忘記將原先的拿掉)，
- 解決方案: 註解掉 BackgroundService中的Home 流程

## 問題2: Motor本身的Alarm沒顯示出來
- 發生現狀: Motor本身的Alarm只有在手動頁面顯示,修改增加到Error顯示地方
- 動作: MonitoringService有修改狀態的部分，在CheckSystemStatus()中增加確認動作

# 20251029

## 執行程式

### 目前執行方式

sudo nano /etc/systemd/system/charger-control-app.service

```sh
[Unit]
Description=Charger Control App
After=setup-serial-can.service

[Service]
Type=simple
ExecStart=/home/moxa/program/testapp2/publish/ChargerControlApp
Restart=always
User=moxa

[Install]
WantedBy=multi-user.target
```

- 啟用: sudo systemctl enable charger-control-app.service

### 出現問題

1. 找不到 appsettings.json - 解法: 執行下列命令
```bash
cp -f /home/moxa/program/testapp2/publish/appsettings.json /home/moxa/appsettings.json
```
後續要加到執行檔中

2. service無法執行 <br>
下達 service查詢命令
```bash
systemctl status charger-control-app.service
```
結果
```bash
charger-control-app.service - Charger Control App
     Loaded: loaded (/etc/systemd/system/charger-control-app.service; enabled; vendor preset: enabled)
     Active: failed (Result: exit-code) since Wed 2025-10-29 15:12:25 GMT; 2min 20s ago
    Process: 798 ExecStart=/home/moxa/program/testapp2/publish/ChargerControlApp (code=exited, status=131)
   Main PID: 798 (code=exited, status=131)
        CPU: 13ms
```

下達查詢log命令
```bash
journalctl -u charger-control-app.service
```

結果
```bash
Failed to search journal ACL: Operation not supported
-- Journal begins at Tue 2025-10-21 02:47:22 GMT, ends at Wed 2025-10-29 15:12:34 GMT. --
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: You must install .NET to run this application.
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: App: /home/moxa/program/testapp2/publish/ChargerControlApp
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: Architecture: arm64
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: App host version: 8.0.17
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: .NET location: Not found
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: Learn more:
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: https://aka.ms/dotnet/app-launch-failed
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: Download the .NET runtime:
Oct 29 15:12:24 moxa-imoxa1000046 ChargerControlApp[722]: https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=arm64&rid=linux-arm64&os=debian.11&apphost_version=8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: You must install .NET to run this application.
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: App: /home/moxa/program/testapp2/publish/ChargerControlApp
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: Architecture: arm64
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: App host version: 8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: .NET location: Not found
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: Learn more:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: https://aka.ms/dotnet/app-launch-failed
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: Download the .NET runtime:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[752]: https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=arm64&rid=linux-arm64&os=debian.11&apphost_version=8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: You must install .NET to run this application.
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: App: /home/moxa/program/testapp2/publish/ChargerControlApp
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: Architecture: arm64
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: App host version: 8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: .NET location: Not found
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: Learn more:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: https://aka.ms/dotnet/app-launch-failed
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: Download the .NET runtime:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[784]: https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=arm64&rid=linux-arm64&os=debian.11&apphost_version=8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: You must install .NET to run this application.
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: App: /home/moxa/program/testapp2/publish/ChargerControlApp
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: Architecture: arm64
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: App host version: 8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: .NET location: Not found
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: Learn more:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: https://aka.ms/dotnet/app-launch-failed
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: Download the .NET runtime:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[791]: https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=arm64&rid=linux-arm64&os=debian.11&apphost_version=8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: You must install .NET to run this application.
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: App: /home/moxa/program/testapp2/publish/ChargerControlApp
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: Architecture: arm64
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: App host version: 8.0.17
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: .NET location: Not found
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: Learn more:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: https://aka.ms/dotnet/app-launch-failed
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: Download the .NET runtime:
Oct 29 15:12:25 moxa-imoxa1000046 ChargerControlApp[798]: https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=arm64&rid=linux-arm64&os=debian.11&apphost_version=8.0.17
```

預計解法:
- 修正 charger-control-app.service
```sh
[Unit]
Description=Charger Control App
After=network.target

[Service]
Type=simple
User=moxa
WorkingDirectory=/home/moxa/program/testapp2/publish
ExecStart=/home/moxa/program/testapp2/publish/ChargerControlApp
Restart=on-failure
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

- 讓環境知道dotnet<br>
建立軟連結到 /usr/local/bin
```bash
sudo ln -s /opt/dotnet/dotnet /usr/local/bin/dotnet
```
測試
```bash
dotnet --info
```

重啟服務
```bash
sudo systemctl restart charger-control-app.service
```


## 預計修改項目

### CANBUS 測試

- 問題: CANBUS同步的function沒有 timeout，所以所以會卡住
  - 解法1. 把所有CANBUS下達命令根據 不同設備分成不同Task....但能否重複接收則要測試看看
  - 解法2. 把同一個CANBUS Port的 READ() 獨立一個Task.....SEND流程的TASK內部計算Timeout....如果Timeout就進行下一個 SEND....只要有回應 READ就會讀到值....讓SEND的流程去分析
  - 解法3. 非同步程序Debug後測試

- 問題: Slot狀態機與 CANBUS綁在一起
  - 解法: 獨立一個Task給Slot狀態機，或是上述問題處理完

### Motor Error 串接到 Error 狀態機

- 問題: 目前 Motor Alarm是獨立狀態會在運行中改成Error State，平時狀態不會
  - 解法: 平時狀態新增改變機制
- 問題: 目前 Motor Alarm 無顯示錯誤訊息
  - 解法: 加到錯誤訊息中

### Initial 狀態
- 問題: 初始化狀態會進行2次
  - 先查找原因

### Home Page 問題
- 問題: 有些按鈕按下去沒反應
  - 先查找原因

### 狀態機
- 問題: 跟其他模組有相互阻礙的問題
  - 先查找原因
