using ChargerControlApp.Utilities;
using Grpc.Core;
using Grpc.Net.Client;
using Nexano.FMS.DeviceController.Protos;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChargerControlApp.Services
{
    public class GrpcClientService
    {
        private readonly GrpcChannel _grpcChannel;
        private readonly DeviceRegistrationService.DeviceRegistrationServiceClient _client;
        private readonly AppSettings _settings;
        private readonly ILogger<GrpcClientService> _logger;
        private static readonly ConcurrentQueue<string> _logMessages = new();
        public static IEnumerable<string> LogMessages => _logMessages;

        public static bool IsOnline { get; internal set; } = false;
        public GrpcClientService(GrpcChannel grpcChannel, ILogger<GrpcClientService> logger)
        {
            _grpcChannel = grpcChannel;
            _client = new DeviceRegistrationService.DeviceRegistrationServiceClient(grpcChannel);
            ConfigLoader.Load();
            _settings = ConfigLoader.GetSettings();
            _settings = _settings ?? new AppSettings();
            _logger = logger;
        }

        private void LogInformation(string message)
        {
            _logger.LogInformation(message);
            _logMessages.Enqueue(message);
            while (_logMessages.Count > 1000) // 保持隊列大小不超過1000條
            {
                _logMessages.TryDequeue(out _);
            }
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
                LogInformation($"[gRPC] DevicePostRegistration Response: UUID={response.RequestUuid}, DeviceName={response.DeviceName}, HeartbeatIpPort={response.HeartbeatIpPort}");
                if (_settings.GRPCRegisterOnlyResponse) IsOnline = true;
                else if(response.DeviceName == _settings.DeviceName) IsOnline = true; // 註冊成功後，設置為在線狀態
                //response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"gRPC Error: {ex.Message}");
                LogInformation($"[gRPC] DevicePostRegistration Error: {ex.Message}");
                return new DevicePostRegistrationResponse
                {
                    RequestUuid = 0,
                    DeviceName = "",
                    HeartbeatIpPort = "",

                };
            }
        }

        public async Task<DeviceServiceSimpleResponse> DeleteRegisterAsync()
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
                LogInformation($"[gRPC] DeviceDeleteRegistration Response: RequestId={response.RequestId}, Success={response.Success}, Message={response.Message}");
                if (response.Success) IsOnline = false; // 成功刪除註冊後，設置為離線狀態
                return response;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"gRPC Error: {ex.Message}");
                LogInformation($"[gRPC] DeviceDeleteRegistration Error: {ex.Message}");
                return new DeviceServiceSimpleResponse
                {
                    RequestId = 0,
                    Success = false,
                    Message = "",

                };
            }
        }

        public async Task<bool> ManualOnline()
        {
            int bypassCounter = 0;
            bool result = false;

            DevicePostRegistrationResponse devicePostRegistrationResponse;
            do
            {
                bypassCounter++;

                try
                {
                    devicePostRegistrationResponse = await RegisterDeviceAsync();
                    if (IsOnline)
                    {
                        result = true;
                        LogInformation($"[gRPC] Device Manual Online Success. Device Name={devicePostRegistrationResponse.DeviceName}");
                        break; // 註冊成功，跳出迴圈
                    }
                    else
                    { 
                        LogInformation($"[gRPC] Device Manual Online Attempt {bypassCounter} Failed.");
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"註冊過程中發生錯誤: {ex.Message}");
                    LogInformation($"[gRPC] Device Manual Online Error: {ex.Message}");
                    //return; // 錯誤時退出
                    break; // 先跳出迴圈，進入 Idle 狀態 => ToDo: 待上傳gRPC完成後再修改
                }

                await Task.Delay(2000); // 進行輪詢
            } while (bypassCounter < 3);

            return result;
        }

        public async Task<bool> ManualOffline()
        {
            try
            {
                var response = await DeleteRegisterAsync();
                if (response.Success)
                {
                    LogInformation($"[gRPC] Device Manual Offline Success.");
                    return true;
                }
                else
                {
                    LogInformation($"[gRPC] Device Manual Offline Failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogInformation($"[gRPC] Device Manual Offline Error: {ex.Message}");
                return false;
            }
        }
    }

}
