using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Xml;

namespace WowLauncher
{
    public partial class Form1 :
        Form
    {
        private BindingList<Server> _servers;
        private bool _isValidRealmlistPath = false;
        private bool _isValidGamePath = false;

        public Form1()
        {
            InitializeComponent();
            if (Properties.Settings.Default.RealmlistPath.Length == 0)
            {
                var installPath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Blizzard Entertainment\World of Warcraft", "InstallPath", null);
                if (installPath != null)
                {
                    Properties.Settings.Default.RealmlistPath =
                        Path.Combine((string)installPath, @"Data\ruRu\realmlist.wtf");
                }
            }
            if (Properties.Settings.Default.GamePath.Length == 0)
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
            Location = Properties.Settings.Default.WindowLocation;
            Size = Properties.Settings.Default.WindowSize;
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
        }
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            ShowContent();
        }
        private void Servers_AddingNew(object sender, AddingNewEventArgs e)
        {
            e.NewObject = new Server { ServerName = "Новый сервер", FileContent = "" };
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
            openRealmlistDialog.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.RealmlistPath);
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
            if (dataGridView1.CurrentRow != null)
            {
                Server server = _servers[dataGridView1.CurrentRow.Index];
                server.FileContent = realmlistTextBox.Text;

                if (_isValidRealmlistPath)
                {
                    File.WriteAllText(Properties.Settings.Default.RealmlistPath, realmlistTextBox.Text);
                }
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
            openGameDialog.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.GamePath);
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
                string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Notepad.lnk";
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = server.ServerName;
                shortcut.TargetPath = Process.GetCurrentProcess().MainModule.FileName;
                shortcut.Arguments = "\"" + server.FileContent.Trim().Replace(Environment.NewLine, "{newline}") + "\"";
                shortcut.Save();
            }
        }
        private void shortCutButton_Click(object sender, EventArgs e)
        {
            CreateShortcut();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
