using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using CallCentre.Models;
using IdentityModel.OidcClient;
using NAudio.Wave;
using Newtonsoft.Json;

namespace CallCentre
{
    public partial class Main : Form
    {
        private Process _process;
        private SipAccount _account;
        private OidcClient _oidcClient;
        private LoginResult _loginResult;

        private readonly string _microSipPath = Path.Combine(Environment.CurrentDirectory, "MicroSip");
        private readonly string _microSipConfigPath = Path.Combine(Environment.CurrentDirectory, "MicroSip", "microsip.ini");

        protected override CreateParams CreateParams
        {
            get
            {
                var myCp = base.CreateParams;
                myCp.ClassStyle |= 0x200;
                return myCp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            CheckDevices();
            base.WndProc(ref m);
        }


        public Main()
        {
            InitializeComponent();

            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.Start(Constants.UpdateUrl);

            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            versionLabel.Text = $"Версія: {version}";

            LoginUser();
        }

        private async void LoginUser()
        {
            const string redirectUri = "http://127.0.0.1:7890/";

            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUri);
            httpListener.Start();

            try
            {
                var options = new OidcClientOptions
                {
                    Authority = Constants.ApiUrl,
                    ClientId = Constants.ClientId,
                    Scope = Constants.Scope,
                    RedirectUri = redirectUri
                };

                _oidcClient = new OidcClient(options);
                var state = await _oidcClient.PrepareLoginAsync();
                OpenBrowser(state.StartUrl);

                var context = await httpListener.GetContextAsync();
                BringFormToFront();
                await SendResponseAsync(context);

                _loginResult = await _oidcClient.ProcessResponseAsync(context.Request.RawUrl, state);
            }
            catch (Exception exception)
            {
                httpListener.Stop();

                var result = MessageBox.Show(exception.Message, "Помилка", MessageBoxButtons.RetryCancel);
                if (result == DialogResult.Retry)
                {
                    LoginUser();
                }
                else
                {
                    Close();
                }

                return;
            }
            finally
            {
                httpListener.Stop();
                httpListener.Close();
            }

            if (_loginResult.IsError)
            {
                var result = MessageBox.Show(this, _loginResult.Error, "Вхід", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                if (result == DialogResult.Retry)
                {
                    LoginUser();
                }
            }
            else
            {
                try
                {
                    var http = new HttpWrapper(_loginResult.AccessToken);
                    _account = http.Invoke<SipAccount>("GET", Constants.AutoProvisioningUrl, string.Empty);

                    userLabel.Text = _account?.DisplayName;
                    lineLabel.Text = _account?.InternalNumber;

                    CheckDevices();
                    OpenPhone(_account);
                }
                catch
                {
                    OpenPhoneButton.Enabled = true;

                    using (var reserve = new Reserve())
                    {
                        if (reserve.ShowDialog() == DialogResult.OK)
                        {
                            var reserveCode = reserve.richTextBox1.Text;
                            if (!string.IsNullOrEmpty(reserveCode))
                            {
                                var data = Convert.FromBase64String(reserveCode);
                                var config = Encoding.UTF8.GetString(data);
                                var account = JsonConvert.DeserializeObject<SipAccount>(config);

                                if (account.DateTime.Date != DateTime.Now.Date)
                                {
                                    MessageBox.Show("Застосовано невірний код", "Помилка", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                                    Close();
                                }

                                _account = account;

                                userLabel.Text = _account?.DisplayName;
                                lineLabel.Text = _account?.InternalNumber;

                                CheckDevices();
                                OpenPhone(_account);
                            }
                        }
                    }
                }
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ClosePhone();

                var http = new HttpWrapper(_loginResult.AccessToken);
                http.Invoke<object>("POST", Constants.LogoutUrl, string.Empty);
            }
            catch
            {
                // ignored
            }
        }

        private void CheckDevices()
        {
            Invoke(new MethodInvoker(delegate
            {
                var microphone = WaveIn.DeviceCount > 0;
                var headphones = WaveOut.DeviceCount > 0;

                micLabel.Text = microphone ? "Підключено" : "Не під'єднано";
                micLabel.ForeColor = microphone ? Color.ForestGreen : Color.Red;

                handphonesLabel.Text = headphones ? "Підключено" : "Не під'єднано";
                handphonesLabel.ForeColor = headphones ? Color.ForestGreen : Color.Red;
            }));
        }


        #region Phone

        private void OpenPhone(SipAccount account)
        {
            using (var sw = File.CreateText(_microSipConfigPath))
            {
                sw.Write(account?.Settings);
            }

            try
            {
                _process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = _microSipPath,
                        FileName = "microsip.exe"
                    }
                };


                var result = _process.Start();
                if (result)
                {
                    phoneLabel.Text = "Запущено";
                    phoneLabel.ForeColor = Color.ForestGreen;
                    OpenPhoneButton.Enabled = false;
                }
                else
                {
                    phoneLabel.Text = "Не запущено";
                    phoneLabel.ForeColor = Color.Red;
                    MessageBox.Show("Помилка запуску телефону", "Помилка", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    OpenPhoneButton.Enabled = true;
                }


                _process.EnableRaisingEvents = true;
                _process.Exited += Process_Exited;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Помилка запуску телефону", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }
        }

        private void ClosePhone()
        {
            Invoke(new MethodInvoker(delegate
            {
                phoneLabel.Text = "Не запущено";
                phoneLabel.ForeColor = Color.Red;
                OpenPhoneButton.Enabled = true;
            }));

            try
            {
                _process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = _microSipPath,
                        FileName = "microsip.exe",
                        Arguments = "/exit"
                    }
                };
                _process.Start();

                // _process.Kill();
            }
            catch
            {
                MessageBox.Show("Не вдалося закрити модуль 'MicroSip'", "Помилка", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }

            try
            {
                if (File.Exists(_microSipConfigPath))
                    File.Delete(_microSipConfigPath);
            }
            catch
            {
                _process?.Kill();

                Thread.Sleep(1000);

                if (File.Exists(_microSipConfigPath))
                    File.Delete(_microSipConfigPath);
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            ClosePhone();
        }

        #endregion

        #region Buttons

        private void OpenPhoneButton_Click(object sender, EventArgs e)
        {
            OpenPhone(_account);
        }

        private void CloseApplicationButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OpenSoundSettings_Click(object sender, EventArgs e)
        {
            Process.Start("mmsys.cpl");
        }

        #endregion


        private void BringFormToFront()
        {
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private static async Task SendResponseAsync(HttpListenerContext context)
        {
            var response = context.Response;
            const string responseString = "<html><head></head><body>Please return to the app.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ClosePhone();
        }
    }
}