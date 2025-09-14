using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ChargerControlApp.DataAccess.CANBus.Mocks
{
    public class MockCANBusService : ICANBusService
    {
        private readonly ConcurrentQueue<CanMessage> _incomingMessages = new();
        private readonly List<CanMessage> _sentMessages = new();

        public Task SendAsync(CanMessage message)
        {
            _sentMessages.Add(message);

            // 可選：模擬回覆訊息（例如 Echo）
            if (message.Id.Value == 0x100)
            {
                _incomingMessages.Enqueue(new CanMessage
                {
                    Id = new CanId { Value = 0x101, IsExtended = false },
                    Data = new byte[] { 0xAA, 0xBB }
                });
            }

            return Task.CompletedTask;
        }

        public async Task<CanMessage?> ReceiveAsync(int timeoutMs)
        {
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            var start = DateTime.UtcNow;

            while ((DateTime.UtcNow - start) < timeout)
            {
                if (_incomingMessages.TryDequeue(out var message))
                    return message;

                await Task.Delay(10); // 模擬非同步等待
            }

            return null;
        }

        public void SendCommand(byte[] data, uint canid = 0x000C0103)
        {

        }
        public byte[] ReceiveMessage()
        {
            return new byte[2];
        }

        public void EnqueueIncomingMessage(CanMessage message)
        {
            _incomingMessages.Enqueue(message);
        }

        public IReadOnlyList<CanMessage> GetSentMessages() => _sentMessages.AsReadOnly();

        public void ClearCANBuffer()
        {

        }
    }
}
