using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BMP208OwnApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string ConnectionString = "SERVER = MSSQLLocalDB; DATABASE =  myDB;";
        //BMP280 BMP280;
        DispatcherTimer timer;
        int temp = 0;
        //float pressure = 0;
        //float altitude = 0;
        public MainPage()
        {
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_Tick;
            timer.Start();
            
        }
        public void Timer_Tick(object sender, object e)
        {
            temp++;
            temper.Text = temp.ToString();
            //try
            //{
            //    //Create a new object for our barometric sensor class
            //    //BMP280 = new BMP280();
            //    //Initialize the sensor
            //    //await BMP280.Initialize();

            //    //Create variables to store the sensor data: temperature, pressure and altitude. 
            //    //Initialize them to 0.


            //    //Create a constant for pressure at sea level. 
            //    //This is based on your local sea level pressure (Unit: Hectopascal)
            //    //const float seaLevelPressure = 1013.25f;

            //    //Read 10 samples of the data

            //        //temp = await BMP280.ReadTemperature();
            //        //pressure = await BMP280.ReadPreasure();
            //        //altitude = await BMP280.ReadAltitude(seaLevelPressure);

            //        //Write the values to your debug console

            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex.Message);
            //}
            //Debug.WriteLine("Temperature: " + temp.ToString() + " deg C");

            //Debug.WriteLine("Pressure: " + pressure.ToString() + " Pa");
            //pressuar.Text = pressure.ToString();
            //Debug.WriteLine("Altitude: " + altitude.ToString() + " m");
        }
       
    }
}
