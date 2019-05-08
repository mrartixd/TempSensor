using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Security.ExchangeActiveSyncProvisioning;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.System.Threading;

namespace BMP208OwnApp
{
    public sealed partial class MainPage : Page
    {
        //Colors
        SolidColorBrush whitecolor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        SolidColorBrush yellowcolor = new SolidColorBrush(Color.FromArgb(255, 255, 203, 90));

        //DB
        
        DispatcherTimer sendDB;
        SqlCommand cmd;
        const string connecttodb = @"Data Source=mail.vk.edu.ee;Initial Catalog=db_Artur_Shabunov; User Id=t154331; Password=t154331";
        SqlConnection con = new SqlConnection(connecttodb);

        //Driver sensors
        private const string I2C_CONTROLLER_NAME = "I2C1";
        private I2cDevice I2CDev;
        private TSL2561 TSL2561Sensor;
        BMP280 BMP280 = new BMP280();
        DispatcherTimer timer;

        //Values
        private Boolean Gain = false;
        private uint MS = 0;
        public static float temp = 0;
        public static double currentLux = 0;
        public static float pressure = 0;
        public static float altitude = 0;
        const float seaLevelPressure = 1013.25f;

        // Http Server
        private HttpServer WebServer = null;

        //pins
        private GpioPin redpin;
        private GpioPin greenpin;
        private GpioPin bluepin;

        public MainPage()
        {
            this.InitializeComponent();

            //timer for temp
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_Tick;
            timer.Start();
            //

            //timer to send db
            sendDB = new DispatcherTimer();
            sendDB.Interval = TimeSpan.FromSeconds(10);
            sendDB.Tick += sendDB_Tick;
            sendDB.Start();

            //open con
            con.Open();

            // Initialize and Start HTTP Server
            WebServer = new HttpServer();
            var asyncAction = ThreadPool.RunAsync((w) => { WebServer.StartServer(); });
        }

        //check connection to SQL server
        private static bool IsServerConnected(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch (SqlException)
                {
                    return false;
                }
            }
        }

        private async void InitializeI2CDevice()
        {
            try
            {
                // Initialize I2C device
                var settings = new I2cConnectionSettings(TSL2561.TSL2561_ADDR);

                settings.BusSpeed = I2cBusSpeed.FastMode;
                settings.SharingMode = I2cSharingMode.Shared;

                string aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */

                I2CDev = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());

                return;
            }

            initializeSensor();
        }

        private void initializeSensor()
        {
            // Initialize Sensor
            TSL2561Sensor = new TSL2561(ref I2CDev);

            // Set the TSL Timing
            MS = (uint)TSL2561Sensor.SetTiming(false, 2);
            // Powerup the TSL sensor
            TSL2561Sensor.PowerUp();

            Debug.WriteLine("TSL2561 ID: " + TSL2561Sensor.GetId());
            if (TSL2561Sensor.GetId() != 112)
            {
                tslsensor.Text = "Error";
            }
            else
            {
                tslsensor.Text = "2561";
            }

        }

        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            try
            {
                await BMP280.Initialize();
                bmpsensor.Text = "280";
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                bmpsensor.Text = ex.Message;
            }


        }
        public async void Timer_Tick(object sender, object e)
        {
            temp = await BMP280.ReadTemperature();
            pressure = await BMP280.ReadPreasure();
            altitude = await BMP280.ReadAltitude(seaLevelPressure);
            // Retrive luminosity and update the screen
            uint[] Data = TSL2561Sensor.GetData();
            currentLux = TSL2561Sensor.GetLux(Gain, MS, Data[0], Data[1]);
            WriteResults();
        }

        public void sendDB_Tick(object sender, object e)
        {

            cmd = new SqlCommand("insert into [Temp] (Temperature,Lux,Pressure,Altitude,DateTime) values (@temp, @lux, @pres, @altit, @datetime)", con);
            cmd.Parameters.AddWithValue("@temp", temp.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@lux", currentLux.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@pres", pressure.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@altit", altitude.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
            cmd.ExecuteNonQuery();
            Debug.WriteLine("Send to DB: Temp:" + temp.ToString("#####.00") + ", Lux:" + currentLux.ToString("#####.00") + ", Pres:" + pressure.ToString("#####.00") + ", Altit" + altitude.ToString("#####.00"));

            //blue
            redpin.Write(GpioPinValue.Low);
            greenpin.Write(GpioPinValue.Low);
            bluepin.Write(GpioPinValue.High);
            //wait
            Task.Delay(2000).Wait();
            //go back to green
            redpin.Write(GpioPinValue.Low);
            greenpin.Write(GpioPinValue.High);
            bluepin.Write(GpioPinValue.Low);

        }

        public void LuxMethod()
        {
            luxbar1.Value = currentLux;
            luxbar2.Value = currentLux;
            luxbar3.Value = currentLux;

            if (currentLux > 1000)
            {
                luxbar1.Foreground = whitecolor;
                luxbar2.Foreground = whitecolor;
                luxbar3.Foreground = whitecolor;
            }
            else
            {
                luxbar1.Foreground = yellowcolor;
                luxbar2.Foreground = yellowcolor;
                luxbar3.Foreground = yellowcolor;
            }

            if (currentLux <= 0.1)
            {
                lightblack.Visibility = Visibility.Visible;
                luxbar1.Visibility = Visibility.Collapsed;
                luxbar2.Visibility = Visibility.Collapsed;
                luxbar3.Visibility = Visibility.Collapsed;
            }
            else
            {
                lightblack.Visibility = Visibility.Collapsed;
                luxbar1.Visibility = Visibility.Visible;
                luxbar2.Visibility = Visibility.Visible;
                luxbar3.Visibility = Visibility.Visible;
            }
        }

        public void WriteResults()
        {
            temper.Text = temp.ToString("####.00") + " deg C";
            RadialProgressBarControl.Value = temp;
            pressuar.Text = pressure.ToString("#####.00") + " Pa";
            pressurebar.Value = pressure;
            altitudes.Text = altitude.ToString("#####.00") + " m";
            luxer.Text = currentLux.ToString("#####.00" + " lux");

            //animate lamp
            LuxMethod();

            //animate altitude
            if (altitude <= 0)
            {
                rowaltitude.Margin = new Thickness(0, 400, 0, 0);
            }
            else if (altitude >= 200)
            {
                rowaltitude.Margin = new Thickness(0, 200, 0, 0);
            }
            else
            {
                rowaltitude.Margin = new Thickness(0, 400 - altitude, 0, 0);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var gpio = GpioController.GetDefault();
            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                Debug.WriteLine("There is no GPIO controller on this device.");
                gpiostatus.Text = "Check GPIO controller";
                return;
            }

            var deviceModel = GetDeviceModel();
            if (deviceModel == DeviceModel.RaspberryPi2)
            {
                // Use pin numbers compatible with documentation
                const int RPI2_RED_LED_PIN = 5;
                const int RPI2_GREEN_LED_PIN = 13;
                const int RPI2_BLUE_LED_PIN = 6;

                redpin = gpio.OpenPin(RPI2_RED_LED_PIN);
                greenpin = gpio.OpenPin(RPI2_GREEN_LED_PIN);
                bluepin = gpio.OpenPin(RPI2_BLUE_LED_PIN);
            }
            else
            {

                // take the first 3 available GPIO pins
                var pins = new List<GpioPin>(3);
                for (int pinNumber = 0; pinNumber < gpio.PinCount; pinNumber++)
                {
                    // ignore pins used for onboard LEDs
                    switch (deviceModel)
                    {
                        case DeviceModel.DragonBoard410:

                            if (pinNumber == 21 || pinNumber == 120)
                                continue;
                            break;
                    }

                    GpioPin pin;
                    GpioOpenStatus status;
                    if (gpio.TryOpenPin(pinNumber, GpioSharingMode.Exclusive, out pin, out status))
                    {
                        pins.Add(pin);
                        if (pins.Count == 3)
                        {
                            break;
                        }
                    }
                }

                if (pins.Count != 3)
                {
                    Debug.WriteLine("Could not find 3 available pins. This sample requires 3 GPIO pins.");
                    return;
                }

                redpin = pins[0];
                greenpin = pins[1];
                bluepin = pins[2];
            }
            gpiostatus.Text = "OK!";
            //send high value to pins for RGB LED
            redpin.Write(GpioPinValue.High);
            redpin.SetDriveMode(GpioPinDriveMode.Output);
            greenpin.Write(GpioPinValue.High);
            greenpin.SetDriveMode(GpioPinDriveMode.Output);
            bluepin.Write(GpioPinValue.High);
            bluepin.SetDriveMode(GpioPinDriveMode.Output);
            //start initilize I2C
            InitializeI2CDevice();
            modelsbc.Text = deviceModel.ToString();

            if (IsServerConnected(connecttodb) == true)
            {
                redpin.Write(GpioPinValue.Low);
                greenpin.Write(GpioPinValue.High);
                bluepin.Write(GpioPinValue.Low);
                Debug.WriteLine("Connected to DB");
            }
            else
            {
                redpin.Write(GpioPinValue.High);
                bluepin.Write(GpioPinValue.Low);
                greenpin.Write(GpioPinValue.Low);
                Debug.WriteLine("No connection to DB");
            }

        }


        //check GPIO
        public enum DeviceModel { RaspberryPi2, MinnowBoardMax, DragonBoard410, Unknown };

        static DeviceModel GetDeviceModel()
        {
            var deviceInfo = new EasClientDeviceInformation();
            if (deviceInfo.SystemProductName.IndexOf("Raspberry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DeviceModel.RaspberryPi2;
            }
            else if (deviceInfo.SystemProductName.IndexOf("MinnowBoard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DeviceModel.MinnowBoardMax;
            }
            else if (deviceInfo.SystemProductName == "SBC")
            {
                return DeviceModel.DragonBoard410;
            }
            else
            {
                return DeviceModel.Unknown;
            }
        }
    }
}
