<h1> Design </h1>

<h2> 目錄 </h2>


---

# 硬體規劃

- 硬體設計
![Overall](./ChargerControlApp/Pictures/Design/OverallDesign.drawio.svg)

- 點位設計
![Position](./ChargerControlApp/Pictures/Design/Position.drawio.svg)

# 測試流程
## 測試流程1
- 流程Cycle <br>
![Test1Cycle](./ChargerControlApp/Pictures/Design/Test1ProcedureDesign.drawio.svg)
- 初始條件: Car有電池, Slot#1 沒有電池, Slot#2~#4 有電池
- 中途開始條件: Car有電池, Slot#1~#4 需要1個沒有電池,其他三個有電池
- UI可以設定四種模式(測試起始條件)
  - 模式1<br>
  ![Test1_Status1](./ChargerControlApp/Pictures/Test1/Test1_S1.drawio.svg)
  - 模式2<br>
  ![Test1_Status2](./ChargerControlApp/Pictures/Test1/Test1_S2.drawio.svg)
  - 模式3<br>
  ![Test1_Status3](./ChargerControlApp/Pictures/Test1/Test1_S3.drawio.svg)
  - 模式4<br>
  ![Test1_Status4](./ChargerControlApp/Pictures/Test1/Test1_S4.drawio.svg)