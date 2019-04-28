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
        DispatcherTimer timer;
        DispatcherTimer sendDB;
        const float seaLevelPressure = 1013.25f;
        float temp = 0;
        float pressure = 0;
        float altitude = 0;
        BMP280 BMP280 = new BMP280();

        SqlCommand cmd;
        SqlConnection con = new SqlConnection(@"Data Source=mail.vk.edu.ee;Initial Catalog=db_Artur_Shabunov; User Id=t154331; Password=t154331");
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
            //
            con.Open();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            Debug.WriteLine("MainPage::OnNavigatedTo");
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

        }

    }
}
