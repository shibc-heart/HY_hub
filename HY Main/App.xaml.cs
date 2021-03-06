﻿using HY.Application.Base;
using HY.Client.Entity.CommonEntitys;
using HY.Client.Execute.Commons;
using HY.RequestConver.Bridge;
using HY.RequestConver.InterFace;
using HY_Main.Common.Unity;
using HY_Main.Common.UserControls;
using HY_Main.ViewModel.Sign;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HY_Main
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        //http://v.bidsneo.com/hub/index.html#id=sfmm4t&p=%E7%89%88%E6%9C%AC%E8%AF%B4%E6%98%8E
        //http://118.31.16.221:8012/eagle/swagger-ui.html
        //https://www.teambition.com/project/5f1652f562b591d38266aa07/tasks/scrum/5f1652f6911fa600218f9b7f


        public App()
        {
            Process[] pOrange = Process.GetProcessesByName("HY.Main");
            if (pOrange.Length > 1)
            {
                Current.Shutdown(0);
            }
            Task.Run(() => { 
                while (true) {

                    Process[] Fiddler = Process.GetProcessesByName("Fiddler");
                    Process[] Wireshark = Process.GetProcessesByName("Wireshark");
                    if (Fiddler.Length > 0|| Wireshark.Length>0)
                    {
                        Current.Dispatcher.Invoke(new Action(() =>
                        {
                            Current.Shutdown(0);
                        }));
                    }
                    Thread.Sleep(3000);
                }});
            var MacAddress = MacAddressHelper.GetMacByIpConfig() ?? MacAddressHelper.GetMacByWmi().FirstOrDefault() ?? "unknown";
            if (string.IsNullOrEmpty(MacAddress))
            {
                MessageBox.Show("网络连接失败,请您连接网络,稍后重新运行");
                Current.Shutdown();
                return;
            }
            CheckLicence();
            DispatcherUnhandledException += (sender, args) =>
            {
                WriteErrorLog(args.Exception.Message);
            };
            //Task线程未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                WriteErrorLog(args.Exception.Message);
            };

            //
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.IsTerminating)
                {
                    System.Windows.MessageBox.Show("我们很抱歉,当前应用程序遇到一些问题,公共语言运行时已经终止,请重新启动程序,如果还遇到此情况,请联系我们。 ", "应用程序即将终止",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }



        protected override async void OnStartup(StartupEventArgs e)
        {
            var appURL = AppDomain.CurrentDomain.BaseDirectory + "程序更新.txt";
            if (File.Exists(appURL))//判断文件是否存在
            {
                File.Delete(appURL);
            }
            var MAC = CommonsCall.GetDeviceId();
            if (!string.IsNullOrEmpty(MAC))
            {
                Loginer.LoginerUser.macAdd = MAC;
            }
            else
            {
                Loginer.LoginerUser.macAdd = CommonsCall.SetDeviceId();
            }

            ICommon common = BridgeFactory.BridgeManager.GetCommonManager();
            var genrator = await common.GetVersion();
            if (!genrator.code.Equals("000"))
            {
                MessageBox.Show("网络连接失败,请您连接网络,稍后重新运行");
                Current.Shutdown();
                return;
            }
            else
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"DownloadGeam\");
                base.OnStartup(e);
                //IOC接口注册
                BootStrapper.Initialize();
                IModelDialog Dialog;

                var Results = JsonConvert.DeserializeObject<VersionEntity>(genrator.result.ToString());
                var curVersion = Convert.ToInt32(CommonsCall.ReadVersion(Results.version).Replace(".", ""));
                var serverversion = Convert.ToInt32(Results.version.Replace(".", ""));
                if (curVersion != 0 && curVersion < serverversion)
                {
                    UpdateViewModel view = new UpdateViewModel();
                    Dialog = ServiceProvider.Instance.Get<IModelDialog>("UpdateViewDlg");
                    view.InitViewModel(Results);
                    Dialog.BindViewModel(view);
                    await Dialog.ShowDialog();
                }
                else
                {
                    LoginViewModel view = new LoginViewModel();
                    Dialog = ServiceProvider.Instance.Get<IModelDialog>("LoginViewDlg");
                    view.ReadConfigInfo(); //读写配置参数
                    Dialog.BindViewModel(view);
                    await Dialog.ShowDialog();
                }
            }

        }
        /// 获取当前系统的版本号
        /// </summary>
        /// <returns></returns>
        public static string GetEdition()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        /// <summary>
        /// 日志记录
        /// </summary>
        /// <param name="args"></param>
        private void WriteErrorLog(string args)
        {
            try
            {
                string FileName = DateTime.Now.ToString("yyyy年MM月dd日");
                //文件夹路径
                string strPath = AppDomain.CurrentDomain.BaseDirectory + @"Temp\ErrorLog\" + FileName + ".ini";


                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Temp\ErrorLog"))//判断文件夹是否存在
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Temp\ErrorLog");
                    if (!File.Exists(strPath))//判断文件是否存在
                    {
                        File.Create(strPath).Close();
                    }
                }
                List<string> listConfig = new List<string>();
                listConfig.Add(DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"));
                listConfig.Add("\n");
                listConfig.Add(args);
                listConfig.Add("\n\r");
                File.AppendAllLines(strPath, listConfig.ToArray(), Encoding.UTF8);//写入错误配置
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void CheckLicence()
        {
            RegistryKey mainkey = Registry.CurrentUser;
            RegistryKey subkey = mainkey.OpenSubKey("SOFTWARE\\PosRegister\\time", true);
            if (subkey == null)
            {
                object usetime = System.DateTime.Now.AddDays(2).ToLongDateString();
                subkey = mainkey.CreateSubKey("SOFTWARE\\PosRegister\\time");
                subkey.SetValue("Position", usetime);
            }
            try
            {
                DateTime usetime = Convert.ToDateTime(subkey.GetValue("Position"));
                DateTime daytime = DateTime.Parse(System.DateTime.Now.ToLongDateString());
                TimeSpan ts = usetime - daytime;
                int day = ts.Days;
                if (day <= 0)
                {
                    Current.Shutdown();
                }
            }
            catch { }
            subkey.Close();
        }
    }
}
