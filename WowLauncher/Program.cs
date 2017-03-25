using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WowLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (File.Exists(Properties.Settings.Default.RealmlistPath)
                    && File.Exists(Properties.Settings.Default.GamePath))
                {
                    File.WriteAllText(Properties.Settings.Default.RealmlistPath,
                        args[0].Replace("{newline}", Environment.NewLine));
                    Process.Start(Properties.Settings.Default.GamePath);
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
