using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Xml;
using Microsoft.VisualBasic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;

namespace WowLauncher
{
    public partial class Form1 :
        Form
    {
        private BindingList<Server> _servers;
        private bool _isValidRealmlistPath = false;
        private bool _isValidGamePath = false;
        private Socket clientSocket = null;
        private int _selectIndex;
        private int changingIndex;
        private Regex rx = new Regex(@"\S+?\.\S+", RegexOptions.IgnoreCase);

        public Form1()
        {
            InitializeComponent();
            if (Properties.Settings.Default.RealmlistPath.Length == 0 ||
                !File.Exists(Properties.Settings.Default.RealmlistPath))
            {
                var installPath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Blizzard Entertainment\World of Warcraft", "InstallPath", null);
                if (installPath != null)
                {
                    Properties.Settings.Default.RealmlistPath =
                        Path.Combine((string)installPath, @"Data\ruRu\realmlist.wtf");
                }
            }
            if (Properties.Settings.Default.GamePath.Length == 0 ||
                !File.Exists(Properties.Settings.Default.GamePath))
            {
                var gamePath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Blizzard Entertainment\World of Warcraft", "GamePath", null);
                if (gamePath != null)
                {
                    Properties.Settings.Default.GamePath = (string)gamePath;
                }
            }
        }
        private void InitServerList()
        {
            XmlSerializer x = new XmlSerializer(typeof(BindingList<Server>));
            using (TextReader writer = new StreamReader("servers.xml"))
            {
                _servers = (BindingList<Server>)x.Deserialize(writer);
            }

            _servers.AllowNew = true;
            _servers.AllowRemove = true;
            _servers.RaiseListChangedEvents = true;
            _servers.AllowEdit = true;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.WindowLocation.X <=
                SystemInformation.VirtualScreen.Width - 10
                && Properties.Settings.Default.WindowLocation.Y <=
                SystemInformation.VirtualScreen.Height - 10
                && Properties.Settings.Default.WindowLocation.X > 10
                && Properties.Settings.Default.WindowLocation.Y > 10)
            {
                Location = Properties.Settings.Default.WindowLocation;
            }
            if (Properties.Settings.Default.WindowSize.Width <=
                SystemInformation.VirtualScreen.Width
                && Properties.Settings.Default.WindowSize.Height <=
                SystemInformation.VirtualScreen.Height)
            {
                Size = Properties.Settings.Default.WindowSize;
            }
            WindowState = Properties.Settings.Default.State;

            InitServerList();
            filePathTextBox.Text = Properties.Settings.Default.RealmlistPath;
            exeTextBox.Text = Properties.Settings.Default.GamePath;
            serverBindingSource.DataSource = _servers;
            _servers.AddingNew += Servers_AddingNew;
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            ValidateGamePath();
            ValidateRealmlistPath();
            ShowContent();
            for (int i = 0; i < _servers.Count; i++)
            {
                CheckServer(i);
            }
            timer1.Tick += Timer1_Tick;
        }
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                _selectIndex = dataGridView1.CurrentRow.Index;
            }
            ShowContent();
        }
        private void Servers_AddingNew(object sender, AddingNewEventArgs e)
        {
            e.NewObject = new Server { ServerName = "Добавить новый сервер", FileContent = "" };
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                Properties.Settings.Default.State = WindowState;
            }
            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.WindowLocation = Location;
                Properties.Settings.Default.WindowSize = Size;
            }

            Properties.Settings.Default.Save();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineHandling = NewLineHandling.Entitize;

            XmlSerializer x = new XmlSerializer(typeof(BindingList<Server>));
            using (XmlWriter xmlWriter = XmlWriter.Create("servers.xml", settings))
            {
                x.Serialize(xmlWriter, _servers);
            }

            if (_isValidRealmlistPath)
            {
                File.WriteAllText(Properties.Settings.Default.RealmlistPath, realmlistTextBox.Text);
            }
        }
        private void browseButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(Properties.Settings.Default.RealmlistPath))
            {
                openRealmlistDialog.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.RealmlistPath);
            }
            if (openRealmlistDialog.ShowDialog() == DialogResult.OK)
            {
                filePathTextBox.Text = openRealmlistDialog.FileName;
            }
        }
        private void ShowContent()
        {
            if (dataGridView1.CurrentRow != null)
            {
                Server server = _servers[dataGridView1.CurrentRow.Index];
                realmlistTextBox.Text = server.FileContent;
            }
            else
            {
                realmlistTextBox.Text = "";
            }
            if (_isValidRealmlistPath)
            {
                File.WriteAllText(Properties.Settings.Default.RealmlistPath, realmlistTextBox.Text);
            }
        }
        private void SaveContent()
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index == _selectIndex)
            {
                Server server = _servers[dataGridView1.CurrentRow.Index];
                server.FileContent = realmlistTextBox.Text;

                if (_isValidRealmlistPath)
                {
                    File.WriteAllText(Properties.Settings.Default.RealmlistPath, realmlistTextBox.Text);
                }

                if (timer1.Enabled)
                {
                    timer1.Stop();
                }
                CheckServer(dataGridView1.CurrentRow.Index);
            }
        }
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteRowIfNotInEditMode();
            }
        }
        private void DeleteRowIfNotInEditMode()
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentCell != null && !dataGridView1.CurrentCell.IsInEditMode)
            {
                _servers.RemoveAt(dataGridView1.CurrentRow.Index);
            }
        }
        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteRowIfNotInEditMode();
        }
        private bool ValidateRealmlistPath()
        {
            _isValidRealmlistPath = File.Exists(filePathTextBox.Text);
            if (_isValidRealmlistPath)
            {
                panel1.BackColor = DefaultBackColor;
                if (_isValidGamePath)
                {
                    startButton.Enabled = true;
                }
            }
            else
            {
                startButton.Enabled = false;
                panel1.BackColor = Color.Red;
            }
            return _isValidRealmlistPath;
        }
        private bool ValidateGamePath()
        {
            _isValidGamePath = File.Exists(exeTextBox.Text);
            if (_isValidGamePath)
            {
                panel2.BackColor = DefaultBackColor;
                if (_isValidRealmlistPath)
                {
                    startButton.Enabled = true;
                }
                shortCutButton.Enabled = true;
            }
            else
            {
                startButton.Enabled = false;
                shortCutButton.Enabled = false;
                panel2.BackColor = Color.Red;
            }
            return _isValidGamePath;
        }
        private void filePathTextBox_Validating(object sender, CancelEventArgs e)
        {
            ValidateRealmlistPath();
        }
        private void filePathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (ValidateRealmlistPath())
                {
                    startButton.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
        private void exeTextBox_Validating(object sender, CancelEventArgs e)
        {
            ValidateGamePath();
        }
        private void exeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (ValidateGamePath())
                {
                    startButton.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
        private void startButton_Click(object sender, EventArgs e)
        {
            if (_isValidGamePath)
            {
                Process.Start(Properties.Settings.Default.GamePath);
            }
        }
        private void exeBrowseButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(Properties.Settings.Default.GamePath))
            {
                openGameDialog.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.GamePath);
            }
            if (openGameDialog.ShowDialog() == DialogResult.OK)
            {
                exeTextBox.Text = openGameDialog.FileName;
            }
        }
        private void realmlistTextBox_Validating(object sender, CancelEventArgs e)
        {
            SaveContent();
        }
        private void CreateShortcut()
        {
            if (dataGridView1.CurrentRow != null)
            {
                Server server = _servers[dataGridView1.CurrentRow.Index];

                object shDesktop = (object)"Desktop";
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                string shortcutName = Interaction.InputBox("Введите название ярлыка", "Создание ярлыка");
                if (shortcutName.Length > 0)
                {
                    string shortcutAddress = Path.Combine((string)shell.SpecialFolders.Item(ref shDesktop),
                        shortcutName + ".lnk");
                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                    shortcut.Description = server.ServerName;
                    shortcut.TargetPath = Process.GetCurrentProcess().MainModule.FileName;
                    shortcut.Arguments = "\"" + server.FileContent.Trim().Replace(Environment.NewLine, "{newline}") + "\"";
                    shortcut.Save();
                }
            }
        }
        private void shortCutButton_Click(object sender, EventArgs e)
        {
            CreateShortcut();
        }
        private void CheckServer(int index)
        {
            dataGridView1[0, index].Value = "проверка";
            dataGridView1[0, index].Style.ForeColor = DefaultForeColor;

            string name = _servers[index].ServerName;
            var matches = rx.Matches(_servers[index].FileContent);
            if (matches.Count > 0)
            {
                string selectedItem = matches[0].Value;
                try
                {
                    if (selectedItem != "127.0.0.1" && selectedItem != "localhost")
                    {
                        IPAddress hostAddress = Dns.GetHostEntry(selectedItem).AddressList[0];

                        switch (hostAddress.AddressFamily)
                        {
                            case AddressFamily.InterNetwork:
                                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                break;
                            case AddressFamily.InterNetworkV6:
                                clientSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                                break;
                            default:
                                return;
                        }

                        SocketAsyncEventArgs telnetSocketAsyncEventArgs = new SocketAsyncEventArgs();
                        telnetSocketAsyncEventArgs.RemoteEndPoint = new IPEndPoint(hostAddress, 3724); //! Client port is always 3724 so this is safe
                        telnetSocketAsyncEventArgs.Completed += (sender, e) =>
                        {
                            bool online = e.SocketError == SocketError.Success && e.LastOperation == SocketAsyncOperation.Connect;
                            SetSelectedServerState(name, online);
                        };
                        clientSocket.ConnectAsync(telnetSocketAsyncEventArgs);
                    }
                    else
                        //! If server is localhost, check if worldserver is running
                        SetSelectedServerState(name, Process.GetProcessesByName("worldserver").Length > 0 && Process.GetProcessesByName("authserver").Length > 0);
                }
                catch (Exception e)
                {
                    SetSelectedServerState(name, false);
                }
            }
            else
            {
                SetSelectedServerState(index, false);
            }
        }
        private void SetSelectedServerState(string name, bool online)
        {
            int newIndex = _servers.ToList().FindIndex(x => x.ServerName == name);
            SetSelectedServerState(newIndex, online);
        }
        private void SetSelectedServerState(int index, bool online)
        {
            if (online)
            {
                dataGridView1[0, index].Style.ForeColor = Color.Green;
            }
            else
            {
                dataGridView1[0, index].Style.ForeColor = Color.Red;
            }
            dataGridView1[0, index].Value = online ? "онлайн" : "офлайн";
        }
        private void realmlistTextBox_TextChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && ((TextBox)sender).ContainsFocus)
            {
                timer1.Stop();
                changingIndex = dataGridView1.CurrentRow.Index;
                timer1.Start();
            }
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            CheckServer(changingIndex);
            SaveContent();
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < _servers.Count; i++)
            {
                CheckServer(i);
            }
        }
    }
}
