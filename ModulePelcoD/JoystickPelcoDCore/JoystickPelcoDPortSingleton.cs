using System.IO.Ports;

namespace ModulePelcoD.JoystickPelcoDCore
{
    public class JoystickPelcoDPortSingleton
    {
        public SerialPort Port { get; set; }

        private JoystickPelcoDPortSingleton()
        {
            Port = new SerialPort();
        }

        private static readonly Lazy<JoystickPelcoDPortSingleton> instance = new Lazy<JoystickPelcoDPortSingleton>(() => new JoystickPelcoDPortSingleton());

        public static JoystickPelcoDPortSingleton Instance => instance.Value;
    }
}