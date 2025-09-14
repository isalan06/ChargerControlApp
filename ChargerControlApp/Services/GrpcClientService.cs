using ChargerControlApp.Utilities;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TacDynamics.Kernel.DeviceService.Protos; // 這是由 Protobuf 產生的命名空間

namespace ChargerControlApp.Services
{
    public class GrpcClientService
    {
        private readonly GrpcChannel _grpcChannel;
        private readonly DeviceRegistrationService.DeviceRegistrationServiceClient _client;
        private readonly AppSettings _settings;
        public GrpcClientService(GrpcChannel grpcChannel)
        {
            _grpcChannel = grpcChannel;
            _client = new DeviceRegistrationService.DeviceRegistrationServiceClient(grpcChannel);
            ConfigLoader.Load();
            _settings = ConfigLoader.GetSettings();
            _settings = _settings ?? new AppSettings();
        }

        public async Task<DevicePostRegistrationResponse> RegisterDeviceAsync()
        {

            //_settings.ChargingStationName
            DevicePostRegistrationRequest request = new DevicePostRegistrationRequest
            {
                DeviceId = "Asoidjoi",
                DeviceModel = "SPD-1700",
                DeviceName = _settings.ChargingStationName,
                IpPort = "http://192.168.20.203:5009"// ipport
            };
            var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5)); // 設置超時 5 秒

            try
            {
                var response = await _client.PostAsync(request, callOptions);
                //response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"gRPC Error: {ex.Message}");
                return new DevicePostRegistrationResponse
                {
                    Success = false,
                };
            }
        }
    }

}
