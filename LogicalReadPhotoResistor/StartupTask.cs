using System;
using System.Text;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace LogicalReadPhotoResistor
{
	public sealed class StartupTask : IBackgroundTask
    {
		static DeviceClient deviceClient;
		static string iotHubUri = "bathroom-project.azure-devices.net";
		static string deviceName = "bathroom_IoT";
		static string deviceKey = "1gWPtWtBw2LmtcMnJfQmzSzaWvASfz96ISYp7ZD0Ud4=";

		BackgroundTaskDeferral _deferral;
        private const int LED_PIN = 6;
        private const int RESISTOR_PIN = 5;

        private GpioPin ledPin;
        private GpioPin resistorPin;
        private GpioController gpioController = GpioController.GetDefault();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
			SetUpClientDevice();
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

		private void SetUpClientDevice()
		{
			deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey), TransportType.Http1);
		}

        private void PhotoresistorChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var value = sender.Read();
            ledPin.Write(value);
            System.Diagnostics.Debug.WriteLine(value);

			var status = value == GpioPinValue.High ? "Not occupied" : "occupied";

			SendDeviceToCloudMessagesAsync(status);
        }

		private static async void SendDeviceToCloudMessagesAsync(string status)
		{
			var bathroomData = new
			{
				deviceId = deviceName,
				time = DateTime.Now.ToString(),
				roomStatus = status
			};
			var messageString = JsonConvert.SerializeObject(bathroomData);
			var message = new Message(Encoding.ASCII.GetBytes(messageString));

			await deviceClient.SendEventAsync(message);
		}
	}
}
