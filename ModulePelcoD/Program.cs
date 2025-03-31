using System.Numerics;
using ModulePelcoD.Hikvision;

namespace ModulePelcoD
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("START...");

            //JoystickPelcoDService service = new JoystickPelcoDService();
            //service.Start();

            //172.168.10.101 admin VirSign2022
            PtzHttpSender ptzHttpSender = new PtzHttpSender("172.168.10.101", "admin", "VirSign2022");

            var response = await ptzHttpSender.GetCameraInfo();

            var response2 = await ptzHttpSender.SetPosition(0.91F, 0.2F, 0.3F);

            var response3 = await ptzHttpSender.SetPosition(new Vector3(0.1F,0.2F,0));

            var response4 = await ptzHttpSender.GetCameraPTZCtrl(); //PTZChannelList-> PTZ info
        }
    }
}