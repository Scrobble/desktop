using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using NDde.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;

namespace Scobble
{
    public class FolderContent
    {
        public string name;
        public string path;
        public List<string> movies;
        public List<string> books;
        public List<string> musics;
        public List<string> games;

        public FolderContent(string p)
        {
            path = p;

            movies = new List<string>();
            books = new List<string>();
            musics = new List<string>();
            games = new List<string>();
        }

        public override string ToString()
        {
            return (name != null ? name + " - " : "") + path + 
                   "\nVídeos: " + movies.Count +
                   " - Livros: " + books.Count +
                   " - Músicas: " + musics.Count +
                   " - Jogos: " + games.Count;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer timer;

        string netflixID;
        List<FolderContent> folderList = new List<FolderContent>();

        private async void auth()
        {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig("OakxQCXk7UaMVtUpENkxLwgtfoUseJB3BnYYTLXO"));
            var facebookAccessToken = "<login with facebook and get oauth access token>";

            var auth = await authProvider.SignInWithOAuthAsync(FirebaseAuthType.Facebook, facebookAccessToken);

            var firebase = new FirebaseClient("https://scrooble-d7508.firebaseio.com/");
            var dinos = await firebase.Child("dinosaurs").WithAuth(auth.FirebaseToken).OnceAsync<User>();

            foreach (var dino in dinos)
            {
                Console.WriteLine($"{dino.Key}m high.");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            //IFirebaseConfig config = new FirebaseConfig
            //{
            //    AuthSecret = "OakxQCXk7UaMVtUpENkxLwgtfoUseJB3BnYYTLXO",
            //    BasePath = "https://scrooble-d7508.firebaseio.com/"
            //};


            auth();

            vlc.Text = "Não.";
            netflix.Text = "Não.";

            timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(Run);
            timer.Interval = 60000;
            timer.Enabled = true;

            folders.ItemsSource = folderList;

            var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable);
            if (drives != null && drives.ToList().Count > 0)
            {
                for (int i = 0; i < drives.ToList().Count; i++)
                {
                    if (drives.ToList()[i].VolumeLabel.ToLower().Contains("kindle"))
                    {
                        FolderContent fc = new FolderContent(drives.ToList()[i].Name);
                        fc.name = "Kindle";

                        folderList.Add(fc);
                        folders.Items.Refresh();

                        FindFolder(fc);
                    }
                }
            }
        }

        private void Run(object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                vlc.Text = "Não.";
            }));

            TrackBrowserAndApps();
        }
        
        private void TrackBrowserAndApps()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    if (p.MainWindowTitle.ToLower().Contains("vlc"))
                    {
                        Console.WriteLine("VLC: " + p.MainWindowTitle);
                        Dispatcher.Invoke((Action)(() =>
                        {
                            if (!p.MainWindowTitle.Equals("Reprodutor de Mídias VLC"))
                            {
                                vlc.Text = "Sim, " + p.MainWindowTitle.Split(new[] { " - Reprodutor de Mídias VLC" }, StringSplitOptions.None)[0];
                            }
                        }));
                    }
                    else if (p.MainWindowTitle.ToLower().Contains("chrome"))
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle))
                        {
                            if (p.MainWindowTitle.ToLower().Contains("netflix"))
                            {
                                string URL = GetChromeUrl(p);
                                string movieTitle = GetNetflixMovieTitle(URL);

                                if (movieTitle != null)
                                {
                                    if (movieTitle.Equals("browse"))
                                    {
                                        netflixID = null;
                                        Dispatcher.Invoke((Action)(() =>
                                        {
                                            netflix.Text = "Não.";
                                        }));
                                    }
                                    else
                                    {
                                        Console.WriteLine(DateTime.Now.ToString("h:mm:ss") + " - " + movieTitle);

                                        movieTitle += " no Google Chrome";

                                        Dispatcher.Invoke((Action)(() =>
                                        {
                                            if (!netflix.Text.Equals("Não."))
                                                netflix.Text += " e " + movieTitle;
                                            else
                                                netflix.Text = movieTitle;
                                        }));
                                    }
                                }
                            }
                            else
                            {
                                netflixID = null;
                                Dispatcher.Invoke((Action)(() =>
                                {
                                    netflix.Text = "Não.";
                                }));
                            }
                        }
                    }
                    else if (p.MainWindowTitle.ToLower().Contains("firefox"))
                    {
                        DdeClient dde = new DdeClient("firefox", "WWW_GetWindowInfo");
                        dde.Connect();
                        string url = dde.Request("URL", Int32.MaxValue);
                        dde.Disconnect();

                        Int32 stop = url.IndexOf('"', 1);

                        string URL = url.Substring(1, stop - 1);
                        string Title = url.Substring(stop + 3, url.Length - stop - 8);

                        if (Title.ToLower().Contains("netflix"))
                        {
                            string movieTitle = GetNetflixMovieTitle(URL);

                            if (movieTitle != null)
                            {

                                if (movieTitle.Equals("browse"))
                                {
                                    netflixID = null;
                                    Dispatcher.Invoke((Action)(() =>
                                    {
                                        netflix.Text = "Não.";
                                    }));
                                }
                                else
                                {
                                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss") + " - " + movieTitle);

                                    movieTitle += " no Firefox";

                                    Dispatcher.Invoke((Action)(() =>
                                    {
                                        if (!netflix.Text.Equals("Não."))
                                            netflix.Text += " e " + movieTitle;
                                        else
                                            netflix.Text = movieTitle;
                                    }));
                                }
                            }
                        }
                        else
                        {
                            netflixID = null;
                            Dispatcher.Invoke((Action)(() =>
                            {
                                netflix.Text = "Não.";
                            }));
                        }
                    }
                }
            }
        }

        private string GetNetflixMovieTitle(string url)
        {
            string id = url.Split('?')[0].Split('/').Last();

            if (id.Equals("browse"))
            {
                return id;
            }
            else
            {
                if (netflixID == null || !netflixID.Equals(id))
                {
                    netflixID = id;

                    WebRequest request = WebRequest.Create("http://localhost:3000/api/netflix/" + id);
                    WebResponse response = request.GetResponse();

                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);

                    string responseData = reader.ReadToEnd();

                    reader.Close();
                    response.Close();

                    return responseData;
                }
                else
                {
                    return null;
                }
            }
        }

        public static string GetChromeUrl(Process process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            if (process.MainWindowHandle == IntPtr.Zero)
                return null;

            AutomationElement element = AutomationElement.FromHandle(process.MainWindowHandle);
            if (element == null)
                return null;

            AutomationElementCollection edits5 = element.FindAll(TreeScope.Subtree, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
            AutomationElement edit = edits5[0];

            return ((ValuePattern)edit.GetCurrentPattern(ValuePattern.Pattern)).Current.Value as string;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderContent fc = new FolderContent(folderBrowserDialog1.SelectedPath);

                folderList.Add(fc);
                folders.ItemsSource = folderList;
                folders.Items.Refresh();

                FindFolder(folderList.Last());
            }
        }

        private void FindFolder(FolderContent dir)
        {
            foreach (string file in Directory.EnumerateFiles(dir.path, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".mkv") || file.EndsWith(".mp4"))
                {
                    Console.WriteLine("Movies: " + file);
                    dir.movies.Add(file);
                }
                else if (file.EndsWith(".pdf") || file.EndsWith(".mobi") || file.EndsWith(".epub"))
                {
                    Console.WriteLine("Books: " + file);
                    dir.books.Add(file);
                }
                else if (file.EndsWith(".mp3") || file.EndsWith(".wav") || file.EndsWith(".wmv"))
                {
                    Console.WriteLine("Musics: " + file);
                    dir.musics.Add(file);
                }
            }
        }
    }
}
