using ModulePelcoD.Model;

namespace ModulePelcoD.JoystickPelcoCore
{
    internal class JoystickPelcoDService
    {
        private JoystickPelcoDPortWorker _joystickPelcoDPortWorker;
        private JoystickPelcoDReader _joystickPelcoReader;

        private Task<PortConverter> _findPortTask;
        private Task _openPortTask;
        private Task _readPortTask;

        private readonly CancellationToken _token;

        public JoystickPelcoDService()
        {
            var cancelTokenSource = new CancellationTokenSource();
            _token = cancelTokenSource.Token;

            _joystickPelcoReader = new JoystickPelcoDReader();
            _joystickPelcoDPortWorker = new JoystickPelcoDPortWorker();

            _findPortTask = new Task<PortConverter>(_joystickPelcoDPortWorker.FindPort, _token);

            _openPortTask = _findPortTask.ContinueWith(task => { _joystickPelcoDPortWorker.OpenPort(task.Result); }, _token);

            _readPortTask = new Task(_joystickPelcoReader.ReadPort, _token);
            Start();
        }

        public void Start()
        {
            try
            {
                _findPortTask.Start();

                _readPortTask.Start();

                // Не дает консолиакрыться
                _openPortTask.Wait(_token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public СoordinatesPelcoD GetValue()
        {
            return _joystickPelcoReader.CoordinateValue;
        }
    }
}