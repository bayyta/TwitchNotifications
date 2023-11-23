using System;
using System.Windows;
using System.Windows.Threading;

namespace TwitchNotificationsWPF
{
    /// <summary>
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        private string url;

        private DispatcherTimer m_timer = new DispatcherTimer(), t_timer = new DispatcherTimer();
        private System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();

        private int s_width, s_height, width, height, taskbarheight;
        private int speed = 10;
        private int uptime = 7000;

        bool back = false;

        public Popup(string url, string display_name, string logo, bool speak)
        {
            if (speak)
            {
                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.CurrentThread.IsBackground = true;
                    System.Speech.Synthesis.SpeechSynthesizer ss = new System.Speech.Synthesis.SpeechSynthesizer();
                    ss.Speak(display_name + " just went live");
                }).Start();
            }

            // init components
            InitializeComponent();
            System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(logo);
            bi.EndInit();
            image_channel_logo.Source = bi;

            // TODO: hard coded, fix 
            width = 242;
            height = 72;

            // channel
            this.url = url;
            label_channel_live.Content = display_name;
            if (display_name.Length >= 18) label_channel_live.FontSize = 14;

            // dimensions
            s_width = (int)SystemParameters.PrimaryScreenWidth;
            s_height = (int)SystemParameters.PrimaryScreenHeight;
            Left = s_width - width;
            Top = s_height;
            taskbarheight = (int)(SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height);

            // timer
            t_timer.Tick += new EventHandler(Timer_Tick);
            t_timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            t_timer.Start();
        }

        private void Popup1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            t_timer.Stop();
            time.Stop();
            Program.active_popups--;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!back)
            {
                if (Top > s_height - height - taskbarheight)
                {
                    Top -= speed;
                    if (Top < s_height - height - taskbarheight) Top = s_height - height - taskbarheight;
                }
                else
                {
                    back = true;
                    time.Start();
                }
            }
            else
            {
                if (time.ElapsedMilliseconds > uptime) Top += speed;
                if (Top > s_height) Close();
            }
        }

        private void Popup1_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("HELLO");
            System.Diagnostics.Process.Start(url);
        }

        private void Popup1_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            System.Diagnostics.Process.Start(url);
        }
    }
}
