using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;

namespace RP_Analog_Conversion
{
    public sealed partial class MainPage
    {
        private int READY_PIN = 5;

        private GpioPin _inGpioPin;

        private I2cDevice _converter;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await InitializeAsync();
        }

        private async void InGpioPinOnValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            // Read conversion register
            // Lire le registre de conversion
            var bytearray = new byte[2];
            _converter.WriteRead(new byte[] { 0x0 }, bytearray);

            // Convert to int16
            // Converti en int16
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytearray);

            var value = BitConverter.ToInt16(bytearray, 0);

            // Voltage = (value * gain)/372767
            // Volt = (value * gain)/372767
            var volt = (value * 2.048) / 32767.0;

            // Temperature = (volt - 0.5) * 100
            var temp = (volt - .5) * 100;

            Debug.WriteLine($"Volt : {volt}  temp : {temp:f2}");
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => TheTextBlock.Text = $"Temperature {temp:f2}°C");
        }

        private async Task InitializeAsync()
        {
            // Initializing the ADS 1115
            // Initialisation du ADS 1115
            var i2CSettings = new I2cConnectionSettings(0x48)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                SharingMode = I2cSharingMode.Shared
            };

            var i2C1 = I2cDevice.GetDeviceSelector("I2C1");

            var devices = await DeviceInformation.FindAllAsync(i2C1);

            _converter = await I2cDevice.FromIdAsync(devices[0].Id, i2CSettings);

            // Write in the config register. 0xc4 0Xe0 = ‭1100010011100000‬
            // = listen to A0, default gain, continuous conversion, 860 SPS, assert after one conversion (= READY signal) 
            // see http://www.adafruit.com/datasheets/ads1115.pdf p18 for details
            // Ecrit dans le registre config. 0xc4 0Xe0 = ‭1100010011100000‬
            // = ecouter A0, gain par défaut, conversion continue, 860 SPS, signal READY après chaque conversion 
            // see http://www.adafruit.com/datasheets/ads1115.pdf p18 for details
            _converter.Write(new byte[] { 0x01, 0xc4, 0xe0 });
            // Configure the Lo_thresh (0x02) and Hi_Thresh (0x03) registers so the READY signal will be sent
            // Configure les registres Lo_thresh (0x02) et Hi_Thresh (0x03) pour que le signal READY soit envoyé
            _converter.Write(new byte[] { 0x02, 0x00, 0x00 });
            _converter.Write(new byte[] { 0x03, 0xff, 0xff });

            // Instanciate the READY pin and listen to change 
            // Instancie la broche REASY et écoute les changements
            var gpio = GpioController.GetDefault();
            _inGpioPin = gpio.OpenPin(READY_PIN);

            _inGpioPin.ValueChanged += InGpioPinOnValueChanged;

        }
    }
}
