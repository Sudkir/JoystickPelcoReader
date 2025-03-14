namespace ModulePelcoD.Model
{

    /// <summary>
    /// Структура пакета Pelco-D получаемого с джостика<br/>
    /// Structure of Pelco-D packet received from joystick
    /// </summary>
    public class PacketPelcoD
    {
        public PacketPelcoD()
        {
        }

        public PacketPelcoD(byte[] packet)
        {
            if (packet.Length != 7) return;
            Address = packet[1];
            Command1 = packet[2];
            Command2 = packet[3];
            SpeedX = packet[4];
            SpeedY = packet[5];
            Checksum = packet[6];
        }

        /// <summary>
        /// Всегда 0xFF (синхронизация начала сообщения)<br/>
        /// Always 0xFF (message start sync)
        /// </summary>
        public byte SyncByte { get; set; } = 0xFF;

        /// <summary>
        /// Логический адрес устройства (1–255, 0x01–0xFF)<br/>
        /// Logical address of the device (1–255, 0x01–0xFF)
        /// </summary>
        public byte Address { get; set; }

        /// <summary>
        /// Битовая маска для базовых функций (фокус, ирис, включение камеры и т.д.)<br/>
        /// Bitmask for basic functions (focus, iris, camera on, etc.)
        /// </summary>
        public byte Command1 { get; set; }

        /// <summary>
        /// Битовая маска для направления движения (вверх/вниз, влево/вправо, зум)<br/>
        /// Bitmask for direction of movement (up/down, left/right, zoom)
        /// </summary>
        public byte Command2 { get; set; }

        /// <summary>
        /// Скорость панорамирования<br/>
        /// Panning speed 
        /// </summary>
        public byte SpeedX { get; set; }

        /// <summary>
        /// Скорость наклона<br/>
        /// Tilt speed
        /// </summary>
        public byte SpeedY { get; set; }

        /// <summary>
        /// Контрольная сумма: сумма байтов 2–6 по модулю 256 ((B2+B3+B4+B5+B6) % 256)<br/>
        /// Checksum: sum of bytes 2–6 modulo 256 ((B2+B3+B4+B5+B6) % 256)
        /// </summary>
        public byte Checksum { get; set; }

        /// <summary>
        /// Проверяет совпадает ли контрольная сумма<br/>
        /// Checks if the checksum matches
        /// </summary>
        public bool isValid
        {
            get
            {
                var sum = (byte)((Address + Command1 + Command2 + SpeedX + SpeedY) % 256);
                return sum == Checksum;
            }
        }
    }
}