using ChargerControlApp.Utilities;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nexano.FMS.DeviceController.Protos;

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
            var grpcHwVersions = _settings.HwVersions
                .Select(h => new Nexano.FMS.DeviceController.Protos.HardwareVersion
                {
                    Name = h.Name,
                    Version = h.Version,
                    SerialNumber = h.SerialNumber
                })
                .ToList();
            var grpcSWVersions = _settings.SwVersions
                .Select(s => new Nexano.FMS.DeviceController.Protos.SoftwareVersion
                {
                    Name = s.Name,
                    Version = s.Version
                })
                .ToList();

            //_settings.ChargingStationName
            DevicePostRegistrationRequest request = new DevicePostRegistrationRequest
            {
                RequestUuid = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0),
                DeviceId = _settings.DeviceId,
                IpPort = _settings.IPPort,
                DeviceModel = _settings.DeviceModel,
                HwVersions = { grpcHwVersions },
                SwVersions = { grpcSWVersions},
                DeviceInfo = _settings.DeviceInfo,
                TagName = _settings.TagName,
                DeviceName = _settings.DeviceName,

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
                    RequestUuid = 0,
                    DeviceName = "",
                    HeartbeatIpPort = "",

                };
            }
        }

        public async Task<DeviceServiceSimpleResponse> DeleteRegisterAs()
        {
            DeviceDeleteRegistrationRequest request = new DeviceDeleteRegistrationRequest
            {
                RequestUuid = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0),
                DeviceId = _settings.DeviceId,
            };

            var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5)); // 設置超時 5 秒

            try
            {
                var response = await _client.DeleteAsync(request, callOptions);
                //response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"gRPC Error: {ex.Message}");
                return new DeviceServiceSimpleResponse
                {
                    RequestId = 0,
                    Success = false,
                    Message = "",

                };
            }
        }
    }

}
