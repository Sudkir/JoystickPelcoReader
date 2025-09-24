using System.Numerics;
using ModulePelcoD.Hikvision;
using ModulePelcoD.JoystickPelcoCore;

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
            PtzHttpSender ptzHttpSender = new PtzHttpSender("172.168.10.55", "admin", "VirSign2022");

            var response = await ptzHttpSender.GetCameraInfo();

            var response2 = await ptzHttpSender.SetPresetSpeed(8);

            var response3 = await ptzHttpSender.GetCameraInfo();

            //var response2 = await ptzHttpSender.SetPosition(60F, 0F, 0F);

            //var response3 = await ptzHttpSender.SetPosition(new Vector3(0.1F, 0.2F, 0));

            var response4 = await ptzHttpSender.GetCameraPTZCtrl(); //PTZChannelList-> PTZ info




            // var response5 = await ptzHttpSender.SetPreset(1);

            // var response2 = await ptzHttpSender.SetPosition(60F, 60F, 0F);

            var response6 = await ptzHttpSender.CallPreset(1);


            Thread.Sleep(4000);

            var response7 = await ptzHttpSender.CallPreset(2);

            Thread.Sleep(4000);

            var response8 = await ptzHttpSender.CallPreset(1);

            Thread.Sleep(4000);

            var response9 = await ptzHttpSender.CallPreset(2);
        }
    }
}