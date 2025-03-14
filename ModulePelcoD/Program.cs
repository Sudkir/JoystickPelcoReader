using ModulePelcoD.JoystickPelcoCore;

namespace ModulePelcoD
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("START...");

            JoystickPelcoDService service = new JoystickPelcoDService();
            service.Start();
        }
    }
}