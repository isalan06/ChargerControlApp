<h1> 開發隨記 </h1>

# 20251114

## 優化1: CANBUS寫法
- 起源: 目前用同步的方法，但將Send跟Read分開兩個執行緒，只要Send的命令間隔較長就比較不會有問題，但不能確認後續的環境狀況會不會讓Read執行Cycle Time超過Send的命令間隔，因此看是否有更好的寫法
- 優化想法: 在Send的序列中不用考慮回應，但依然需要有Send的時間間隔，且需要做一個定期掃描哪一個DeviceID一直沒有回應，要減少該ID的發送命令次數

## 優化2: 自動流程動作增加
- 起源: 有時候會需要中斷流程，之後再繼續，另外有發現在Test1流程下狀態機是Idle
- 確認事項: 先確定 主流程跟子流程是否容易增加，及狀態機

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
