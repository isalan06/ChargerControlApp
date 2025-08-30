using ChargerControlApp.DataAccess.CANBus.Interfaces;
using ChargerControlApp.DataAccess.CANBus.Models;
using Microsoft.Extensions.Logging;
using SocketCANSharp;
using SocketCANSharp.Network;
using SocketCANSharp.Network.Netlink;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace ChargerControlApp.DataAccess.CANBus.Linux
{
    public class SocketCANBusService : ICANBusService
    {
        private readonly RawCanSocket _socket;

        public SocketCANBusService()
        {
            string canInterfaceName = "can0";
            _socket = new RawCanSocket();
            CanNetworkInterface _canBus = CanNetworkInterface.GetAllInterfaces(true).First(iface => iface.Name.Equals(canInterfaceName));
            _socket.Bind(_canBus);
            //_socket.Bind(new CanSocketAddress(interfaceName));
        }

        /// <summary>
        /// TODO: Async有問題，沒檢查
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public async Task<CanMessage?> ReceiveAsync(int timeoutMs)
        {
            return await Task.Run(() =>
            {
                CanFrame frame = default;

                IntPtr fdSet = Marshal.AllocHGlobal(128);
                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)fdSet.ToPointer();
                        for (int i = 0; i < 128; i++) ptr[i] = 0;

                        int fd = _socket.SafeHandle.DangerousGetHandle().ToInt32();
                        ptr[fd / 8] |= (byte)(1 << (fd % 8));

                        var timeout = new Timeval
                        {
                            tv_sec = timeoutMs / 1000,
                            tv_usec = (timeoutMs % 1000) * 1000
                        };

                        int result = select(fd + 1, ref fdSet, IntPtr.Zero, IntPtr.Zero, ref timeout);

                        if (result == 0)
                            return (CanMessage?)null;

                        if (result < 0)
                            throw new IOException("select() failed");

                        int num = LibcNativeMethods.Read(_socket.SafeHandle, ref frame, Marshal.SizeOf(typeof(CanFrame)));
                        if (num == -1)
                            throw new IOException("CAN socket read failed");

                        return new CanMessage
                        {
                            Id = CanId.FromRaw(frame.CanId),
                            Data = frame.Data.Take(frame.Length).ToArray()
                        };
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(fdSet);
                }
            });
        }

        /// <summary>
        /// TODO: Async有問題，還沒檢查
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendAsync(CanMessage message)
        {
            CanFrame frame = new()
            {
                CanId = message.Id.ToRaw(),
                Length = (byte)message.DLC,
                Data = new byte[8]
            };

            Array.Copy(message.Data, frame.Data, Math.Min(8, message.Data.Length));
            return Task.Run(() => _socket.Write(frame));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Timeval
        {
            public int tv_sec;
            public int tv_usec;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int select(int nfds, ref IntPtr readfds, IntPtr writefds, IntPtr exceptfds, ref Timeval timeout);

        public void SendCommand(byte[] data)
        {
            uint canId = 0x000C0103 | 0x80000000; // 确保是 29-bit 扩展帧
            if (data.Length <= 8)
            {
                var frame = new CanFrame(canId, data); // 标准 CAN
                //Console.WriteLine("STD CAN: " + frame.ToString());
                _socket.Write(frame);
            }
            else if (data.Length <= 64)
            {
                var frame = new CanFdFrame(canId, data, CanFdFlags.CANFD_FDF); // CAN FD
                //Console.WriteLine("CAN FD: " + frame.ToString());
                _socket.Write(frame);
            }
            else
            {
                Console.WriteLine("错误: 数据长度超出 CAN FD 限制!");
            }
        }

        public byte[] ReceiveMessage()
        {
            try
            {
                var frame = _socket.Read(out CanFrame canFrame);
                return canFrame.Data;
            }
            catch (Exception ex)
            {
                //_logger.LogError("CANbus ReceiveMessageError: ", ex.Message);

                //Console.WriteLine($"Error while receiving CAN message: {ex.Message}");
                return null; // 返回 null 或其他适当的错误值
            }

        }
        /// <summary>
        /// 清空 CAN bus 接收 buffer，避免讀到舊資料。
        /// </summary>
        //private const int CAN_FRAME_SIZE = 16;

        public void ClearCANBuffer()
        {
            try
            {
                _socket.Blocking = false;
                while (true)
                {
                    try
                    {
                        CanFrame frame = new CanFrame();
                        int bytesRead = _socket.Read(out frame);// (buffer, 0, buffer.Length);

                        if (bytesRead <= 0)
                            break;

                        // 若 CanFrame 沒有 byte[] constructor，就自己 parse
                        //var frame = new CanFrame();
                        //frame.Update(buffer); // 有些版本需要這樣填入資料

                        Console.WriteLine($"清除 Frame: ID=0x{frame.CanId:X}, Data={BitConverter.ToString(frame.Data)}");
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                        // 已無資料可讀，清空完成
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除 CAN buffer 時發生錯誤: {ex.Message}");
            }
            finally
            {
                _socket.Blocking = true;
            }
        }
    }
}
