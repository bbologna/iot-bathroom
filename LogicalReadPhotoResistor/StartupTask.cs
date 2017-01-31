using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using System.Threading;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace LogicalReadPhotoResistor
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private const int LED_PIN = 6;
        private const int RESISTOR_PIN = 5;

        private GpioPin ledPin;
        private GpioPin resistorPin;
        private GpioController gpioController = GpioController.GetDefault();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            SetupLed();
            SetupPhotoresistor();
        }

        private void SetupLed()
        {
            ledPin = gpioController.OpenPin(LED_PIN);
            ledPin.Write(GpioPinValue.Low);
            ledPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private void SetupPhotoresistor()
        {
            resistorPin = gpioController.OpenPin(RESISTOR_PIN);
            if (resistorPin.IsDriveModeSupported(GpioPinDriveMode.InputPullDown))
                resistorPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
            else
                resistorPin.SetDriveMode(GpioPinDriveMode.Input);
            resistorPin.DebounceTimeout = new TimeSpan(100);
            resistorPin.ValueChanged += PhotoresistorChanged;
        }

        private void PhotoresistorChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var value = sender.Read();
            ledPin.Write(value);
            System.Diagnostics.Debug.WriteLine(value);
        }

    }
}
