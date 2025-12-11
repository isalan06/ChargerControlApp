using ChargerControlApp.DataAccess.CANBus.Models;
using ChargerControlApp.DataAccess.GPIO.Services;

namespace ChargerControlApp.Test.Function
{
    public class CanRouteCommandFrameTest
    {
        public CanRouteCommandFrameList[] CanRouteCommandFrameList = new CanRouteCommandFrameList[] {
            new CanRouteCommandFrameList(), new CanRouteCommandFrameList(), new CanRouteCommandFrameList(), new CanRouteCommandFrameList()
        };

        private CancellationTokenSource source = new CancellationTokenSource();
        private Task DoWork()
        {
            CancellationToken ct = source.Token;


            return Task.Run(async () =>
            {
                int count = 0;

                while (!ct.IsCancellationRequested)
                {
                    for(int i=0;i<CanRouteCommandFrameList.Length;i++)
                    {
                        var commandList = CanRouteCommandFrameList[i];
                        CanRouteCommandFrame? command = new CanRouteCommandFrame();
                        bool isFinal = false;
                        bool result = commandList.Next(out command, out isFinal);

                        Console.WriteLine($"List {i} - Next Command Result: {result}, Command Index: {commandList?.CommandIndex}, Is Final: {isFinal}, IsTimeout: {commandList?.IsReadTimeout}, ElapsedTime: {commandList?.ElapsedTime_ms}");
                        


                    }

                    if (count++ >= 15)
                    {
                        count = 0;

                        
                    }

                    if(count == 3)
                        CanRouteCommandFrameList[1].CaptureResponse(Hardware.NPB450Controller.CanbusReadCommand.READ_VOUT);

                    if(count == 6)
                        CanRouteCommandFrameList[1].CaptureResponse(Hardware.NPB450Controller.CanbusReadCommand.READ_IOUT);

                    if (count == 9)
                        CanRouteCommandFrameList[1].CaptureResponse(Hardware.NPB450Controller.CanbusReadCommand.CHG_STATUS);

                    if (count == 12)
                        CanRouteCommandFrameList[1].CaptureResponse(Hardware.NPB450Controller.CanbusReadCommand.FAULT_STATUS);


                    Console.WriteLine("----");

                    await Task.Delay(2000); // Adjust the delay as needed
                }
            }, ct);
        }

        public void Start()
        { 
            DoWork();
        }
    }
}
