using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using ModulePelcoD.JoystickPelcoDCore;
using ModulePelcoD.Model;
using System.IO.Ports;
using System.Management;

namespace ModulePelcoD
{
    internal class JoystickPelcoDPortWorker : IDisposable
    {
        private string _portName;
        private string _shortPortName;
        private int _portBaudRate;

        private CancellationTokenSource _cancelTokenSource;
        private readonly CancellationToken _token;
        private bool _isFound = false;

        private static AutoResetEvent OpenPortEvent = new AutoResetEvent(false);

        private static AutoResetEvent FindPortEvent = new AutoResetEvent(false);

        public JoystickPelcoDPortWorker()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IConfigurationSection section = config.GetSection("Settings");

            _portName = section.GetSection("PortName").Value; 
            _portBaudRate = Convert.ToInt32(section.GetSection("BaudRate").Value);
            _shortPortName = section.GetSection("ShortPortName").Value;
        }

        private bool CheckPortName(string? name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentException("COM port not found!", name);

            if (name.IndexOf("COM", StringComparison.Ordinal) > -1) return true;

            throw new ArgumentException("Not correct COM port name!", name);
        }

        public void OpenPort(PortConverter port)
        {
            JoystickPelcoDPortSingleton.Instance.Port = new SerialPort();
            while (true)
            {
                try
                {
                    if (_token.IsCancellationRequested) break;

                    if (JoystickPelcoDPortSingleton.Instance.Port is { IsOpen: true }) continue;

                    JoystickPelcoDPortSingleton.Instance.Port = new SerialPort(
                        portName: port.Name,
                        baudRate: _portBaudRate,
                        parity: Parity.None,
                        dataBits: 8,
                        stopBits: StopBits.One);
                    JoystickPelcoDPortSingleton.Instance.Port.Open();
                    OpenPortEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    if (!_token.IsCancellationRequested)
                    {
                        OpenPortEvent.WaitOne(1000);
                    }
                }
            }
        }

        public PortConverter FindPort()
        {
            while (true)
            {
                try
                {
                    if (_token.IsCancellationRequested) break;

                    List<PortConverter> portConverters = new List<PortConverter>();

                    using (ManagementClass i_Entity = new ManagementClass("Win32_PnPEntity"))
                    {
                        const string CUR_CTRL = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\";
                        var t = i_Entity.GetInstances();
                        foreach (ManagementObject i_Inst in i_Entity.GetInstances())
                        {
                            Object o_Guid = i_Inst.GetPropertyValue("ClassGuid");
                            if (o_Guid == null || o_Guid.ToString().ToUpper() != "{4D36E978-E325-11CE-BFC1-08002BE10318}")
                                continue; // Skip all devices except device class "PORTS"

                            var s_Serv = i_Inst.GetPropertyValue("Service").ToString();
                            var s_Description = i_Inst.GetPropertyValue("Caption").ToString();
                            var s_Manufact = i_Inst.GetPropertyValue("Manufacturer").ToString();
                            var s_DeviceID = i_Inst.GetPropertyValue("PnpDeviceID").ToString();
                            var s_RegEnum = CUR_CTRL + "Enum\\" + s_DeviceID + "\\Device Parameters";
                            var s_RegServ = CUR_CTRL + "Services\\BTHPORT\\Parameters\\Devices\\";
                            var s_PortName = Registry.GetValue(s_RegEnum, "PortName", "").ToString();

                            var s32_Pos = s_Description.IndexOf(" (COM");
                            if (s32_Pos > 0) // remove COM port from description
                                s_Description = s_Description.Substring(0, s32_Pos);

                            PortConverter converter = new PortConverter()
                            {
                                Name = s_PortName,
                                Description = s_Description,
                                DeviceID = s_DeviceID,
                                Manufacturer = s_Manufact,
                                Service = s_Serv
                            };
                            portConverters.Add(converter);
                        }
                    }
                 
                    var port = portConverters.First(x => x.DeviceID.Contains(_shortPortName));

                    if (CheckPortName(port.Name))
                    {
                        FindPortEvent.Set();
                        return port;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    if (!_token.IsCancellationRequested)
                    {
                        FindPortEvent.WaitOne(1000);
                    }
                }
            }

            return new PortConverter();
        }

        public void Dispose()
        {
            _cancelTokenSource.Cancel();
        }
    }
}