using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows;

namespace TwitchNotificationsWPF
{
    class Program
    {
        private bool running = false;
        private WebClient m_WebClient;
        private List<string> liveChannels = new List<string>(), stopped = new List<string>();
        private FollowsObject fo;
        private string username, last_channel_online = "";
        private bool init = true, stop = false, shouldCheck = false, enough_time = false;
        private bool sound = false, isSpeaking = false;
        private Stopwatch time, timePaused, timeAfterPopup;
        private string checkUsername = "";
        private System.Speech.Synthesis.SpeechSynthesizer ss;

        public static int active_popups = 0;

        public Program()
        {
            ss = new System.Speech.Synthesis.SpeechSynthesizer();
            ss.SpeakCompleted += new EventHandler<System.Speech.Synthesis.SpeakCompletedEventArgs>(SpeakCompleted);
        }

        public void NewUser(string username)
        {
            Debug.WriteLine(username);
            this.username = username;
            init = true;
        }

        public void Start()
        {
            Debug.WriteLine("START");
            stop = false;
            if (timePaused.IsRunning)
            {
                if (timePaused.ElapsedMilliseconds > 60000)
                {
                    if (!init) init = true;
                }
                timePaused.Reset();
            }
        }

        public void Stop()
        {
            Debug.WriteLine("STOP");
            stop = true;
            timePaused.Start();
        }

        private void ItemClick(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripMenuItem t = (System.Windows.Forms.ToolStripMenuItem)sender;
            if (t.Checked)
            {
                stopped.Add(t.Text);
                t.Checked = false;
            }
            else
            {
                stopped.Remove(t.Text);
                t.Checked = true;
            }
        }

        private void Init(System.Windows.Forms.ContextMenuStrip cms, bool newuser)
        {
            liveChannels.Clear();
            // Get the users followed channels in an object. Only need to be done once
            var url = "https://api.twitch.tv/kraken/users/" + username + "/follows/channels";
            string json = null;
            while (json == null)
            {
                if (!running || init) return;
                try
                {
                    json = m_WebClient.DownloadString(url);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                    Thread.Sleep(1000);
                }
            }
            fo = JsonConvert.DeserializeObject<FollowsObject>(json);

            // context menu stuff (bit hard coded)
            if (newuser)
            {
                stopped.Clear();
                Debug.WriteLine(cms.Items.Count);
                int m = cms.Items.Count - 4;
                for (int i = 0; i < m; i++)
                {
                    Debug.WriteLine(i);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        cms.Items.RemoveAt(2);
                    });
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < fo.follows.Count; i++)
                    {
                        cms.Items.Insert(2 + i, new System.Windows.Forms.ToolStripMenuItem(fo.follows[i].channel.display_name, null, ItemClick));
                        ((System.Windows.Forms.ToolStripMenuItem)cms.Items[2 + i]).Checked = true;
                    }
                    if (fo.follows.Count != 0) cms.Items.Insert(2 + fo.follows.Count, new System.Windows.Forms.ToolStripSeparator());
                    else
                    {
                        cms.Items.Insert(2 + fo.follows.Count, new System.Windows.Forms.ToolStripMenuItem("No followed channels!"));
                        cms.Items.Insert(3 + fo.follows.Count, new System.Windows.Forms.ToolStripSeparator());
                    }
                });
            }
            // First, see who is online at the moment. Must be done before we can loop to see who goes online
            for (int i = 0; i < fo.follows.Count; i++)
            {
                int tries = 0;
                bool restart = false;
                json = null;
                url = "https://api.twitch.tv/kraken/streams/" + fo.follows[i].channel.name;
                while (json == null)
                {
                    if (!running || init) return;
                    try
                    {
                        json = m_WebClient.DownloadString(url);
                    }
                    catch (Exception e)
                    {
                        tries++;
                        Debug.WriteLine(e.StackTrace);
                        Thread.Sleep(1000);
                        // if number of tries is over 10, then startover
                        if (tries >= 20 && i != 0)
                        {
                            i = -1;
                            liveChannels.Clear();
                            Debug.WriteLine("Restarting!");
                            restart = true;
                            break;
                        }
                    }
                }
                if (!running || init) return;
                if (restart)
                {
                    i = -1;
                    continue;
                }
                StreamObject so = JsonConvert.DeserializeObject<StreamObject>(json);
                if (so.stream == null)
                {
                    Debug.WriteLine(fo.follows[i].channel.display_name + " is OFFLINE!");
                }
                else
                {
                    // Add online channels to list but don't do popups because we don't know if they just went online or not
                    Debug.WriteLine(fo.follows[i].channel.display_name + " is ONLINE!");
                    liveChannels.Add(fo.follows[i].channel.name);
                }
            }
        }

        public void Run(string username, System.Windows.Forms.ContextMenuStrip cms)
        {
            m_WebClient = new WebClient();
            time = new Stopwatch();
            time.Start();
            timePaused = new Stopwatch();
            timeAfterPopup = new Stopwatch();
            Debug.WriteLine(username);
            this.username = username;
            running = true;
            while (running)
            {
                if (timeAfterPopup.IsRunning)
                {
                    if (timeAfterPopup.ElapsedMilliseconds < 5 * 60 * 1000)
                    {
                        if (!enough_time) enough_time = true;
                    }
                    else
                    {
                        enough_time = false;
                        timeAfterPopup.Reset();
                    }
                }
                if (stop)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                if (init)
                {
                    init = false;
                    Init(cms, true);
                    time.Restart();
                }
                else if (time.ElapsedMilliseconds > 30 * 60 * 1000)
                {
                    init = false;
                    Init(cms, false);
                    time.Restart();
                }
                if (!running) return;
                if (stop) continue;
                // Do every 20 seconds
                Thread.Sleep(20000);
                Debug.WriteLine("Checking...");
                // Go through every followed channel and see if online
                for (int i = 0; i < fo.follows.Count; i++)
                {
                    string json = null;
                    var url = "https://api.twitch.tv/kraken/streams/" + fo.follows[i].channel.name;
                    // Don't proceed until json string is done, indicating wether channel is live or not. Then deserialize it
                    while (json == null)
                    {
                        if (!running) return;
                        if (init || stop) break;
                        try
                        {
                            json = m_WebClient.DownloadString(url);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.StackTrace);
                            Thread.Sleep(1000);
                        }
                    }
                    if (!running) return;
                    if (init || stop) break;
                    StreamObject so = JsonConvert.DeserializeObject<StreamObject>(json);

                    // Check if channel is online or not
                    if (so.stream == null)
                    {
                        // Channel is offline
                        // ** //
                        if (liveChannels.Contains(fo.follows[i].channel.name))
                        {
                            // Channel just went offline because the channel is still in the "liveChannels" list
                            Debug.WriteLine(fo.follows[i].channel.display_name + " just went OFFLINE!");
                            liveChannels.Remove(fo.follows[i].channel.name);

                            last_channel_online = fo.follows[i].channel.display_name;
                            if (!timeAfterPopup.IsRunning) timeAfterPopup.Start();
                            else timeAfterPopup.Restart();
                        }
                    }
                    else
                    {
                        // Channel is online
                        // ** //
                        if (!liveChannels.Contains(fo.follows[i].channel.name))
                        {
                            // Channel just went online because the channel is not in the "liveChannels" list
                            liveChannels.Add(fo.follows[i].channel.name);
                            Debug.WriteLine(fo.follows[i].channel.display_name + " just went ONLINE!");
                            // New popup window to notify user
                            if (!stopped.Contains(fo.follows[i].channel.display_name))
                            {
                                if (!(last_channel_online == fo.follows[i].channel.display_name && enough_time))
                                {
                                    string url_logo;
                                    if (so.stream.channel.logo != null) url_logo = so.stream.channel.logo;
                                    else url_logo = "http://static-cdn.jtvnw.net/jtv_user_pictures/xarth/404_user_150x150.png";
                                    NewPopup(fo.follows[i].channel.url, fo.follows[i].channel.display_name, url_logo, sound);
                                }
                            }
                        }
                    }
                }
            }
        }

        public List<string> getFollowedChannels()
        {
            List<string> l = new List<string>();
            for (int i = 0; i < fo.follows.Count; i++)
            {
                l.Add(fo.follows[i].channel.display_name);
            }
            return l;
        }

        public void setRunning(bool b)
        {
            running = b;
        }

        // Should have used BackgroundWorker
        public void CheckIfUsernameExists(System.Windows.Controls.Image load, System.Windows.Controls.Image bock, System.Windows.Controls.Image cross)
        {
            WebClient wc = new WebClient();
            string username = "";
            while (shouldCheck)
            {
                if (checkUsername == "")
                {
                    username = "";
                }
                if (checkUsername == username)
                {
                    Thread.Sleep(30);
                    continue;
                }
                username = checkUsername;
                var url = "https://api.twitch.tv/kraken/users/" + username;
                string result = null;
                try
                {
                    result = wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        //var resp = (HttpWebResponse)ex.Response;
                        //if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                        //{
                        //}
                        if (username == checkUsername && shouldCheck)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                load.Visibility = Visibility.Hidden;
                                cross.Visibility = Visibility.Visible;
                            });
                        }
                        continue;
                    }
                    if (!shouldCheck) return;
                    Debug.WriteLine(ex.StackTrace);
                    Thread.Sleep(20);
                }
                if (username == checkUsername && shouldCheck)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        load.Visibility = Visibility.Hidden;
                        bock.Visibility = Visibility.Visible;
                    });
                }
            }
        }

        private void NewPopup(string name, string display_name, string logo, bool speak)
        {
            active_popups++;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Popup popup = new Popup(name, display_name, logo, speak);
                popup.Owner = Application.Current.MainWindow;
                popup.Show();
            });
        }

        public void setShouldCheck(bool b)
        {
            shouldCheck = b;
        }

        public void setCheckUsername(string s)
        {
            checkUsername = s;
        }

        public string getCheckUsername()
        {
            return checkUsername.ToLower();
        }

        public string getUsername()
        {
            return username;
        }

        private void SpeakCompleted(object sender, System.Speech.Synthesis.SpeakCompletedEventArgs e)
        {
            Debug.WriteLine("SPEAK COMPLETED");
            isSpeaking = false;
        }

        public void ToggleSound()
        {
            if (!isSpeaking)
            {
                sound = !sound;
                isSpeaking = true;
                new Thread(() =>
                {
                    if (sound) ss.SpeakAsync("Text to speech activated");
                    else ss.SpeakAsync("Text to speech deactivated");
                }).Start();
            }
        }
    }
}
