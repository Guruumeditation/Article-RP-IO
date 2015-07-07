using System;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace RP_DigitalPin
{
    public sealed partial class MainPage
    {
        private int TRIGGER_PIN = 5; // Trigger => GPIO5
        private int ECHO_PIN = 6; // Echo => GPIO6

        private GpioPin _triggerPin;
        private GpioPin _echoPin;

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InitGpio();
            // Check distance every second.
            // Mesure la distance chaque seconde.

            _timer.Tick += (sender, o) =>
            {
                var distance = GetDistance();
                var s = $"Distance : {distance} cm";
                Debug.WriteLine(s);
                TheTextBlock.Text = s;
            };

            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Start();

        }
        /// <summary>
        /// Initialize GPIO pins
        /// Initialise les pin GPIO
        /// </summary>
        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();

            _echoPin = gpio.OpenPin(ECHO_PIN);
            _triggerPin = gpio.OpenPin(TRIGGER_PIN);

            _echoPin.SetDriveMode(GpioPinDriveMode.Input);
            _triggerPin.SetDriveMode(GpioPinDriveMode.Output);

            _triggerPin.Write(GpioPinValue.Low);
            var value = _triggerPin.Read();
        }

        public double GetDistance()
        {
            var mre = new ManualResetEventSlim(false);
           
            //Send a 10µs pulse to start the measurement
            //Envoie une pulsion de 10µs pour commencer le calcul 
            _triggerPin.Write(GpioPinValue.High);
            mre.Wait(TimeSpan.FromMilliseconds(0.01));
            _triggerPin.Write(GpioPinValue.Low);

            var time = PulseIn(_echoPin, GpioPinValue.High);

            // multiply by speed of sound in milliseconds (34000) divided by 2 (cause pulse make rountrip)
            // multiplie par la vitesse du son en millisecondes (34000) divisé par 2 (parce que l'impulsion fait un aller-retour)
            var distance = time*17000;

            return distance;
        }
        /// <summary>
        /// Mimic the PulseIn Arduino command. Returns, in ms, the pulse duration time.
        /// Mimique la commande Arduino PulseIn. Retourne, en ms, la durée de l'impulsion.
        /// </summary>
        /// <param name="pin">The pin to read / Le pin a lire</param>
        /// <param name="value">The pulse value / La valeur de l'impulsion</param>
        /// <returns>Pulse duration in ms / Durée de l'impulsion en ms</returns>
        private double PulseIn(GpioPin pin, GpioPinValue value)
        {
            var sw = new Stopwatch();
            // Wait for pulse
            // Attend l'impulsion
            while (pin.Read() != value)
            {
            }
            sw.Start();

            // Wait for pulse end
            // Attend la fin de l'impulsion
            while (pin.Read() == value)
            {
            }
            sw.Stop();

            return sw.Elapsed.TotalSeconds;
        }
    }
}
