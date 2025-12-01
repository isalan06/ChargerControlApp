using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Models;
using Microsoft.Extensions.Logging;
using SocketCANSharp;
using SocketCANSharp.Network;
using SocketCANSharp.Network.Netlink;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChargerControlApp.DataAccess.CANBus.Linux
{
    public class SocketCANBusService_test : ICANBusService, IDisposable
    {
        private RawCanSocket _socket;
        private readonly string _ifName;
        private readonly object _ioLock = new object(); // 單一 I/O 鎖，保護所有對 _socket 的操作

        public SocketCANBusService_test()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            _ifName = "can0";
            OpenAndBindSocket();
        }

        private void OpenAndBindSocket()
        {
            lock (_ioLock)
            {
                try
                {
                    _socket?.Close();
                    (_socket as IDisposable)?.Dispose();
                }
                catch { /* ignore */ }

                _socket = new RawCanSocket();
                var iface = CanNetworkInterface.GetAllInterfaces(true).FirstOrDefault(i => i.Name.Equals(_ifName));
                if (iface == null) throw new InvalidOperationException($"CAN interface {_ifName} not found");
                _socket.Bind(iface);
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (_ioLock)
                {
                    return _socket != null && _socket.Connected;
                }
            }
        }

        /// <summary>
        /// 同步阻塞讀取：保護在 lock 內，供外部呼叫（例如 ChargersReader）使用
        /// </summary>
        public byte[] ReceiveMessage()
        {
            try
            {
                lock (_ioLock)
                {
                    var frame = _socket.Read(out CanFrame canFrame);
                    // 若 Read 方法的回傳不同（某些版本回傳 int 或直接 out），請根據實際 API 微調
                    Console.WriteLine($"接收到 CAN 消息: ID=0x{canFrame.CanId:X}, Data={BitConverter.ToString(canFrame.Data, 0, canFrame.Length)}");
                    int len = Math.Min(canFrame.Length, canFrame.Data.Length);
                    byte[] data = new byte[len];
                    Array.Copy(canFrame.Data, data, len);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving CAN message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 发送命令：写操作在 lock 內，避免與 ClearCANBuffer/ReceiveMessage 競爭
        /// </summary>
        public void SendCommand(byte[] data, uint canid = 0x000C0103)
        {
            uint canId = canid | 0x80000000; // 确保 29-bit 扩展帧

            lock (_ioLock)
            {
                try
                {
                    if (data.Length <= 8)
                    {
                        var frame = new CanFrame(canId, data);
                        Console.WriteLine("STD CAN: " + frame.ToString());
                        _socket.Write(frame);
                    }
                    else if (data.Length <= 64)
                    {
                        var frame = new CanFdFrame(canId, data, CanFdFlags.CANFD_FDF);
                        Console.WriteLine("CAN FD: " + frame.ToString());
                        _socket.Write(frame);
                    }
                    else
                    {
                        Console.WriteLine("错误: 数据长度超出 CAN FD 限制!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SendCommand error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 清空 CAN bus 接收 buffer（在 lock 內操作）
        /// - 將 socket 設為 non-blocking，持續讀直到 EAGAIN/WouldBlock，最後恢復 blocking。
        /// - 因為整個過程在 lock 內，其他會呼叫 ReceiveMessage() 的地方會等到清空完成後再執行。
        /// </summary>
        public void ClearCANBuffer()
        {
            try
            {
                lock (_ioLock)
                {
                    // 切成非阻塞讀
                    _socket.Blocking = false;
                    while (true)
                    {
                        try
                        {
                            // non-blocking read，若無資料會拋出 SocketException 或自訂例外
                            _socket.Read(out CanFrame frame);
                            int len = Math.Min(frame.Length, frame.Data.Length);
                            Console.WriteLine($"清除 Frame: ID=0x{frame.CanId:X}, Data={BitConverter.ToString(frame.Data, 0, len)}");
                            // 繼續讀直到 no data
                        }
                        catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock || se.SocketErrorCode == SocketError.NoData)
                        {
                            // 已無資料可讀，清空完成
                            break;
                        }
                        catch (Exception ex)
                        {
                            // 某些 SocketCANSharp 版本會丟不同的例外或訊息，若訊息含 EAGAIN / temporarily unavailable 視為無資料
                            var msg = ex.Message ?? string.Empty;
                            if (msg.IndexOf("EAGAIN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                msg.IndexOf("temporarily unavailable", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                break;
                            }

                            // 其他例外則記錄並終止清空
                            Console.WriteLine($"清除 CAN buffer 時發生錯誤: {ex}");
                            break;
                        }
                    }

                    // 恢復 blocking 模式
                    _socket.Blocking = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClearCANBuffer failed: {ex}");
            }
        }

        /// <summary>
        /// 同步重建 socket（在 lock 內），可用於在 ENOBUFS/驅動問題時重置
        /// </summary>
        public void ReopenSocket()
        {
            lock (_ioLock)
            {
                try
                {
                    _socket?.Close();
                    (_socket as IDisposable)?.Dispose();
                }
                catch { /* ignore */ }

                _socket = new RawCanSocket();
                var iface = CanNetworkInterface.GetAllInterfaces(true).FirstOrDefault(i => i.Name.Equals(_ifName));
                if (iface == null) throw new InvalidOperationException($"CAN interface {_ifName} not found during reopen");
                _socket.Bind(iface);
            }
        }

        public void Dispose()
        {
            try
            {
                lock (_ioLock)
                {
                    _socket?.Close();
                    (_socket as IDisposable)?.Dispose();
                    _socket = null;
                }
            }
            catch { /* ignore */ }
        }
    }
}