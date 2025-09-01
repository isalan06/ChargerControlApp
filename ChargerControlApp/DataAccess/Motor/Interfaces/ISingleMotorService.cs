using static ChargerControlApp.DataAccess.Motor.Services.SingleMotorService;

namespace ChargerControlApp.DataAccess.Motor.Interfaces
{
    public interface ISingleMotorService
    {
        byte SlaveAddress { get; set; }
        Task<bool> ExecuteRouteProcessOnce();
    }
}
