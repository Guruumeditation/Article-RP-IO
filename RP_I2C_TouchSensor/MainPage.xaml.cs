using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace RP_I2C_TouchSensor
{

    public sealed partial class MainPage
    {
        private I2cDevice _sensor;

        private readonly DispatcherTimer _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private byte _bmp085RegisterCalAc5 = 0xB2; // R   Calibration data (16 bits)
        private byte _bmp085RegisterCalAc6 = 0xB4; // R   Calibration data (16 bits)
        private byte _bmp085RegisterCalMc = 0xBC; // R   Calibration data (16 bits)
        private byte _bmp085RegisterCalMd = 0xBE; // R   Calibration data (16 bits) 

        private byte _bmp085RegisterControl = 0xF4;
        private byte _bmp085RegisterTempdata = 0xF6;

        private byte _bmp085RegisterReadtempcmd = 0x2E;

        private ushort _ac5;
        private ushort _ac6;
        private short _mc;
        private short _md;

        public MainPage()
        {
            InitializeComponent();

            _timer.Tick += (sender, o) =>
            {
                var temp = ReadTemp();
                Debug.WriteLine("Temperature = " + temp);

                TheTextBlock.Text = $"Temperature : {temp}°C";
            };

        }

        private async Task InitializeAsync()
        {
            // Create a connection setting for device address 0x77
            // Créer un paramètre de connection
            var i2CSettings = new I2cConnectionSettings(0x77)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                SharingMode = I2cSharingMode.Shared
            };

            // Get device information for I²C1
            // Retrouve les device information pour le bus I²C1
            var i2c1 = I2cDevice.GetDeviceSelector("I2C1");
            var devices = await DeviceInformation.FindAllAsync(i2c1);
            // Instanciate the sensor
            // Instancie le capteur
            _sensor = await I2cDevice.FromIdAsync(devices[0].Id, i2CSettings);

            _ac5 = ReadUInt16(_bmp085RegisterCalAc5);
            _ac6 = ReadUInt16(_bmp085RegisterCalAc6);
            _mc = ReadInt16(_bmp085RegisterCalMc);
            _md = ReadInt16(_bmp085RegisterCalMd);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await InitializeAsync();

            _timer.Start();
        }

        private double ReadTemp()
        {
            var mre = new ManualResetEventSlim(false);

            Write(_bmp085RegisterControl, _bmp085RegisterReadtempcmd);

            mre.Wait(TimeSpan.FromMilliseconds(5));

            var t = (int)ReadUInt16(_bmp085RegisterTempdata);

            var b5 = ComputeB5(t);
            t = (b5 + 8) >> 4;
            var temp = t / 10.0;

            return temp;
        }

        private int ComputeB5(int value)
        {
            var x1 = (value - _ac6) * _ac5 >> 15;
            var x2 = (_mc << 11) / (x1 + _md);
            return x1 + x2;

        }
        private void Write(byte register, byte command)
        {
            _sensor.Write(new[] { register, command });
        }

        private short ReadInt16(byte register)
        {
            var value = new byte[2];

            _sensor.WriteRead(new[] { register }, value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(value);

            return BitConverter.ToInt16(value, 0);
        }

        private ushort ReadUInt16(byte register)
        {
            var value = ReadInt16(register);

            return (ushort)value;
        }


    }
}

