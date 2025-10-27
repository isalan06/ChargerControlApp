<h1> Flow Chart </h1>

---

# 目錄 
- [Home Procedure](#home-procedure)
- [Semi-Auto Procedure](#semi-auto-procedure)
  - [Rotate Sub-Procedure](#rotate-sub-procedure)
  - [Take Battery Form Car Sub-Procedure](#take-battery-from-car-sub-procedure)
  - [Place Battery To Car Sub-Procedure](#take-battery-from-car-sub-procedure)
  - [Take Battery From Slot Sub-Procedure](#take-battery-from-slot-sub-procedure)
  - [Place Battery To Slot Sub-Procedure](#place-battery-to-slot-sub-procedure)
- [Auto Procedure](#auto-procedure)
- [移動保護](#移動保護)

---

# HOME Procedure
- 原點復歸流程圖
![原點復歸流程圖](./ChargerControlApp/Pictures/FlowChart/HomeFlowChart.drawio.svg)

---

# Semi-Auto Procedure
Robot: 三軸馬達整合定義為動作手臂，用以搬運電池<br>
半自動流程: 規劃 Robot 做一組定義好的動作 <br>
- 單元定義
  - PosFrame: Robot 動作
  - SensorFrame: 感測器檢查邏輯
- 錯誤訊息
  - Error Code: 90 => 動作逾時
  - Error Code: 91 => 動作保護導致無法進行動作
  - Error Code: 92 => 移動後的實際位置與下達命令位置差距過大
  - Error Code: 93 => 手動強制中止
  - Error Code: 94 => 感測器狀態與預期不符合

## Rotate Sub-Procedure
- 旋轉流程圖
![旋轉流程圖](./ChargerControlApp/Pictures/FlowChart/RotateFlowChart.drawio.svg)

---

## Take Battery From Car Sub-Procedure
- 從車上取電池流程圖
![取車上電池流程圖](./ChargerControlApp/Pictures/FlowChart/TakeCarBatteryFlowChart.drawio.svg)

---

## Place Battery To Car Sub-Procedure
- 將電池放置車上流程圖
![放置電池到車上流程圖](./ChargerControlApp/Pictures/FlowChart/PlaceCarBatteryFlowChart.drawio.svg)

---

## Take Battery From Slot Sub-Procedure
- 從 Slot 上取電池流程圖
![取Slot上電池流程圖](./ChargerControlApp/Pictures/FlowChart/TakeSlotBatteryFlowChart.drawio.svg)

- 參數設定
```C#
private static int _z_Input = 3;
private static int _z_Output = 4;
private static int _y_Output = 2;
public static int Z_Input
{
    get { return _z_Input; }
    set
    {
        if (value >= 3 && value <= 18)
        {
            if ((value % 2) == 1)
            {
                _z_Input = value; // Down Position of slot
                _z_Output = _z_Input + 1; // Up Position of slot
            }

            if (value <= 10) _y_Output = 2; else _y_Output = 3; // Y position depends on slot number
        }
    }
}

public static int Z_Output { get { return _z_Output; } }
public static int Y_Output { get { return _y_Output; } }

public static int SlotNo
{
    get
    {
        return (_z_Input - 1) / 2;
    }
}
```

---

## Place Battery To Slot Sub-Procedure
- 將電池放置Slot上流程圖
![放置電池到Slot](./ChargerControlApp/Pictures/FlowChart/PlaceSlotBatteryFlowChart.drawio.svg)

- 參數設定
```C#
private static int _z_Input = 4; 
private static int _z_Output = 3; 
private static int _y_Output = 2;
public static int Z_Input
{
    get { return _z_Input; }
    set
    {
        if (value >= 3 && value <= 18)
        {
            if ((value % 2) == 0)
            {
                _z_Input = value; // Up Position of slot
                _z_Output = _z_Input - 1; // Down Position of slot
            }

            if (value <= 10) _y_Output = 2; else _y_Output = 3; // Y position depends on slot number
        }
    }
}

public static int Z_Output { get { return _z_Output; } }
public static int Y_Output { get { return _y_Output; } }

public static int SlotNo
{
    get
    {
        return (_z_Input - 2) / 2;
    }
}
```

---

# Auto Procedure
全自動流程就是整合半自動流程/Slot管理/電源供應器 <br>
- 全自動流程圖
![全自動流程圖](./ChargerControlApp/Pictures/FlowChart/AutoProcedureFlowChart.drawio.svg)

---

# 移動保護
在半自動流程動作前均需要確認Robot是否可以動作，以避免可能發生碰撞，但點位需要確實教導正確．<br>
另外因為移動阻礙過大導致移動不確實，也可以藉由移動保護來進一步確認<br>
在手動頁面的操作動作是不檢查移動保護的．

程式碼
```C#
public bool CanMove(int axisId)
{
    bool result = false;

    if (axisId == 0) // Rotate Axis
    {
        if (_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
        {
            if (_hardwareManager.Robot.Motors[2].MotorInfo.Pos_Actual >= _hardwareManager.Robot.Motors[2].MotorInfo.OpDataArray[19].Position) // Z Axis at upper limit
            {
                result = true;
            }
        }
        else if (_hardwareManager.Robot.Motors[1].MotorInfo.Pos_Actual < 500)
            result = true; // Y Axis at negative position
    }
    else if (axisId == 1) // Y Axis
    {
        if (_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 })) // Rotate Axis at position 0, 1, 2
        {
            if (_hardwareManager.Robot.InPositions(2, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }))
            {
                result = true;
            }
        }
    }
    else if (axisId == 2) // Z Axis
    {
        if (_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 })) // Rotate Axis at position 0, 1, 2
        {
            if (!_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
            {
                if (_hardwareManager.Robot.ZAxisBetweenSlotOrCar()) // Z Axis between slot or car
                    result = true;
            }
            else
                result = true;

        }
    }

    return result;
}
```
