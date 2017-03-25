using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;

namespace WowLauncher
{
    static class Program
    {
        private static void SendEmail()
        {
            try
            {
                var fromAddress = new MailAddress("maseerin@gmail.com", "WowLauncher");
                var toAddress = new MailAddress("sumrix@gmail.com", "Developer");
                const string fromPassword = "gachagacha";
                const string subject = "WowLauncher Error";
                string body = string.Format("Machine: {0}\nOS Version: {1}\nUser Name: {2}\nApp Directory: {3}",
                    System.Environment.MachineName, Environment.OSVersion, Environment.UserName, AppDomain.CurrentDomain.BaseDirectory);

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    message.Attachments.Add(new Attachment("log.txt"));

                    smtp.EnableSsl = true;
                    smtp.Send(message);
                }
            }
            catch (Exception e)
            {
                LogError(e.ToString());
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                if (args.Length > 0)
                {
                    if (File.Exists(Properties.Settings.Default.RealmlistPath))
                    {
                        if (File.Exists(Properties.Settings.Default.GamePath))
                        {
                            File.WriteAllText(Properties.Settings.Default.RealmlistPath,
                                args[0].Replace("{newline}", Environment.NewLine));
                            Process.Start(Properties.Settings.Default.GamePath);
                        }
                        else
                        {
                            MessageBox.Show("Запускаемый файл WoW по указанному пути \"" +
                                Properties.Settings.Default.GamePath +
                                "\" не найден", "Ошибка запуска WowLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Файл realmflist.wtf по указанному пути \"" +
                            Properties.Settings.Default.RealmlistPath +
                            "\" не найден", "Ошибка запуска WowLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
                throw new Exception("Здоровеньки булы!");
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка выполнения программы WowLauncher.exe!\nОтчёт об ошибке отправлен разработчику.\n" +
                    "Текст ошибки: " + e.ToString(), "Ошибка выполнения WowLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError(e.ToString());
                SendEmail();
            }
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError(((Exception)e.ExceptionObject).ToString());
        }
        public static void LogError(string logMessage)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                w.Write("\r\nError : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("-------------------------------");
            }
        }
    }
}
