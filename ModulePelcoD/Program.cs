using ModulePelcoD.Hikvision;

namespace ModulePelcoD
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {

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
            var response6 = await ptzHttpSender.CallPreset(34);

        }
    }
}