using ModulePelcoD.JoystickPelcoDCore;
using ModulePelcoD.Model;

namespace ModulePelcoD.JoystickPelcoCore
{
    public static class ModTime
    {
        public static long ToUnixTimestampInSeconds(this DateTime dateTime) =>
            (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public static long ToUnixTimestampInMilliseconds(this DateTime dateTime) =>
            (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    public class JoystickPelcoDReader : IDisposable
    {
        public СoordinatesPelcoD CoordinateValue { get; set; }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _token;

        public JoystickPelcoDReader()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _token = _cancellationTokenSource.Token;
            CoordinateValue = new СoordinatesPelcoD();
        }

        public void ReadPort()
        {
            var lastValidTime = DateTime.UtcNow.ToUnixTimestampInSeconds();
            while (true)
            {
                try
                {
                    if (_token.IsCancellationRequested) break;

                    if (JoystickPelcoDPortSingleton.Instance.Port is null ||
                        !JoystickPelcoDPortSingleton.Instance.Port.IsOpen)
                    {
                        ProcessPelcoDPacket(new PacketPelcoD());
                        continue;
                    }

                    var receivedBytes = ReadBuffer(_token);
                    if (receivedBytes.Length > 0)
                    {
                        var receivedBytesList = receivedBytes.ToList();
                        int startIndex = receivedBytesList.IndexOf(0xFF);
                        byte[] packet = receivedBytesList.GetRange(startIndex, 7).ToArray();

                        PacketPelcoD packetD = new PacketPelcoD(packet);

                        if (packetD.isValid)
                            lastValidTime = DateTime.UtcNow.ToUnixTimestampInMilliseconds();
                        ProcessPelcoDPacket(packetD);
                    }
                    else
                    {
                        if (CoordinateValue is { X: 0, Y: 0 }) continue;

                        if (DateTime.UtcNow.ToUnixTimestampInMilliseconds() - lastValidTime > 500)
                        {
                            ProcessPelcoDPacket(new PacketPelcoD());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    Thread.Sleep(25);
                    //   Console.Clear();
                    //Console.WriteLine($"Адрес: {packetD.Address}, Действия: CMD: {packetD.Command2}");
                    Console.WriteLine($"X: {CoordinateValue.X}, Y: {CoordinateValue.Y}, Z: {CoordinateValue.Z}");
                }


            }
        }

        /// <summary>
        /// Чтение пакета из буфера
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public byte[] ReadBuffer(CancellationToken cancellationToken = default)
        {
            if (JoystickPelcoDPortSingleton.Instance.Port.BytesToRead >= 7 && JoystickPelcoDPortSingleton.Instance.Port.BytesToRead <= 14)
            {
                byte[] buffer = new byte[JoystickPelcoDPortSingleton.Instance.Port.BytesToRead];

                JoystickPelcoDPortSingleton.Instance.Port.Read(buffer, 0, JoystickPelcoDPortSingleton.Instance.Port.BytesToRead);

                Console.WriteLine($"{BitConverter.ToString(buffer)}");
                return buffer;
            }

            JoystickPelcoDPortSingleton.Instance.Port.ReadExisting();
            return [];
        }

        private void ProcessPelcoDPacket(PacketPelcoD packetD)
        {
            CoordinateValue = new СoordinatesPelcoD();

            switch (packetD.Command2)
            {
                case 2: //R
                    {
                        CoordinateValue.X = packetD.SpeedX;
                        break;
                    }
                case 4: //L
                    {
                        CoordinateValue.X = packetD.SpeedX * -1;
                        break;
                    }
                case 8: //U
                    {
                        CoordinateValue.Y = packetD.SpeedY;
                        break;
                    }
                case 10: //RU
                    {
                        CoordinateValue.X = packetD.SpeedX;
                        CoordinateValue.Y = packetD.SpeedY;
                        break;
                    }
                case 12: //LU
                    {
                        CoordinateValue.X = packetD.SpeedX * -1;
                        CoordinateValue.Y = packetD.SpeedY;
                        break;
                    }
                case 16: //D
                    {
                        CoordinateValue.Y = packetD.SpeedY * -1;
                        break;
                    }
                case 18: //RD
                    {
                        CoordinateValue.X = packetD.SpeedX;
                        CoordinateValue.Y = packetD.SpeedY * -1;
                        break;
                    }
                case 20: //LD
                    {
                        CoordinateValue.X = packetD.SpeedX * -1;
                        CoordinateValue.Y = packetD.SpeedY * -1;
                        break;
                    }
                case 32: //RotateL
                    {
                        CoordinateValue.Z = 1;

                        break;
                    }
                case 64: //RotateL
                    {
                        CoordinateValue.Z = -1;

                        break;
                    }
                default:
                    {
                        CoordinateValue.X = 0;
                        CoordinateValue.Y = 0;
                        CoordinateValue.Z = 0;
                        break;
                    }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}