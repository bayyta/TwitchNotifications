using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace TwitchNotificationsWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        System.Windows.Forms.NotifyIcon nIcon;
        private System.Windows.Forms.ContextMenuStrip cms;

        private bool initiated = false;

        private Program program;
        private Thread mainThread, checkName;

        public MainWindow()
        {
            InitializeComponent();
            InitNIcon();
            InitCms();
            program = new Program();

            program.setShouldCheck(true);
            checkName = new Thread(() => program.CheckIfUsernameExists(image_loading, image_bock, image_cross));
            checkName.Start();
        }

        #region window events
        private void MainWindow1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Popup popup = new Popup("http://www.google.com", "lirik", "http://static-cdn.jtvnw.net/jtv_user_pictures/xarth/404_user_150x150.png");
            //popup.Owner = Application.Current.MainWindow;
            //popup.Show();
            if (e.ChangedButton == MouseButton.Left)
                Application.Current.MainWindow.DragMove();
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            Close();
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // terminate check-thread
            program.setShouldCheck(false);
            checkName.Join();

            // terminate main thread
            if (mainThread != null)
            {
                Debug.WriteLine("ALIVE");
                program.setRunning(false);
                mainThread.Join();
            }
        }
        #endregion

        #region notifyicon
        private void InitNIcon()
        {
            nIcon = new System.Windows.Forms.NotifyIcon();
            int size = GetSystemMetrics(49);
            nIcon.Icon = new Icon(SystemIcons.Application, size, size);
            nIcon.DoubleClick += new EventHandler(ShowWindow);
            nIcon.Text = "Twitch Notifications";
            nIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            nIcon.BalloonTipTitle = "Twitch Notifications";
            nIcon.BalloonTipText = "Right click notifyicon for options";
        }

        private void ShowWindow(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            Visibility = Visibility.Visible;
            if (WindowState == WindowState.Minimized)
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate ()
                    {
                        WindowState = WindowState.Normal;
                        //textbox_username.Focus();
                    })
                );
        }

        private void InitCms()
        {
            cms = new System.Windows.Forms.ContextMenuStrip();
            cms.Items.Add("Twitch Notifications", null, ShowWindow);
            cms.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            cms.Items.Add("Stop", null, ToggleNotifications);
            cms.Items.Add("Exit", null, ExitApplication);
            nIcon.ContextMenuStrip = cms;
        }

        private void ToggleNotifications(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripItem)sender).Text == "Start")
            {
                ((System.Windows.Forms.ToolStripItem)sender).Text = "Stop";
                program.Start();
            }
            else
            {
                ((System.Windows.Forms.ToolStripItem)sender).Text = "Start";
                program.Stop();
            }
        }
        #endregion

        // close button events
        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // minimize button events
        private void button_minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // logo events
        private void button_logo_Click(object sender, RoutedEventArgs e)
        {
            program.ToggleSound();
            Debug.WriteLine("Hello");
            Program.active_popups++;
            Popup popup = new Popup("sodapoppin", "sodapoppin", "http://static-cdn.jtvnw.net/jtv_user_pictures/lirik-profile_image-c0c34ecdea3ec322-300x300.jpeg", false);
            popup.Owner = Application.Current.MainWindow;
            popup.Show();
        }

        // username textbox
        private void textbox_username_Loaded(object sender, RoutedEventArgs e)
        {
            textbox_username.Focus();
        }

        private void textbox_username_TextChanged(object sender, TextChangedEventArgs e)
        {
            program.setCheckUsername(textbox_username.Text);
            if (textbox_username.Text == "")
            {
                image_loading.Visibility = Visibility.Hidden;
                image_bock.Visibility = Visibility.Hidden;
                image_cross.Visibility = Visibility.Hidden;
            }
            else
            {
                image_loading.Visibility = Visibility.Visible;
                image_bock.Visibility = Visibility.Hidden;
                image_cross.Visibility = Visibility.Hidden;
            }
        }

        private void image_bock_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (image_bock.Visibility == Visibility.Visible) label_enter_start.Visibility = Visibility.Visible;
            else label_enter_start.Visibility = Visibility.Hidden;
        }

        private void textbox_username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (image_bock.IsVisible)
                {
                    Hide(); // Equivalent to Visibility.Hidden
                    nIcon.Visible = true;

                    if (!initiated)
                    {
                        initiated = true;
                        mainThread = new Thread(() => program.Run(program.getCheckUsername(), cms));
                        mainThread.Start();
                        nIcon.ShowBalloonTip(5000);
                    }
                    else
                    {
                        if (program.getUsername() != program.getCheckUsername())
                        {
                            program.NewUser(program.getCheckUsername());
                        }
                        program.Start();
                    }
                }
            }
        }
    }
}
