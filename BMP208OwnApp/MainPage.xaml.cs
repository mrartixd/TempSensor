using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;

namespace BMP208OwnApp
{
    public sealed partial class MainPage : Page
    {
        //DB
        DispatcherTimer timer;
        DispatcherTimer sendDB;
        SqlCommand cmd;
        SqlConnection con = new SqlConnection(@"Data Source=mail.vk.edu.ee;Initial Catalog=db_Artur_Shabunov; User Id=t154331; Password=t154331");
        //Driver and classes
        private const string I2C_CONTROLLER_NAME = "I2C1";
        private I2cDevice I2CDev;
        private TSL2561 TSL2561Sensor;
        BMP280 BMP280 = new BMP280();
        //Values
        private Boolean Gain = false;
        private uint MS = 0;
        float temp = 0;
        double currentLux = 0;
        float pressure = 0;
        float altitude = 0;
        const float seaLevelPressure = 1013.25f;

       
        public MainPage()
        {
            this.InitializeComponent();
            InitializeI2CDevice();


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
            //
            con.Open();
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
        }

        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            try
            {
                await BMP280.Initialize();
                ReadBMP280();
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


        }
        public void Timer_Tick(object sender, object e)
        {
            ReadBMP280();
            // Retrive luminosity and update the screen
            uint[] Data = TSL2561Sensor.GetData();

            currentLux = TSL2561Sensor.GetLux(Gain, MS, Data[0], Data[1]);
        }

        public void sendDB_Tick(object sender, object e)
        {
            cmd = new SqlCommand("insert into [Temp] (Temp,Pres,Altit,DateTime) values (@temp, @pres, @altit, @datetime)", con);
            cmd.Parameters.AddWithValue("@temp", temp.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@pres", pressure.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@altit", altitude.ToString("#####.00"));
            cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
            cmd.ExecuteNonQuery();
        }

        public async void ReadBMP280()
        {

            temp = await BMP280.ReadTemperature();
            pressure = await BMP280.ReadPreasure();
            altitude = await BMP280.ReadAltitude(seaLevelPressure);
           

            temper.Text = temp.ToString("####.00") + " deg C";
            pressuar.Text = pressure.ToString("#####.00") + " Pa";
            altitudes.Text = altitude.ToString("#####.00") + " m";
            luxer.Text = currentLux.ToString("#####.00" + " lux");

        }
    }
}
