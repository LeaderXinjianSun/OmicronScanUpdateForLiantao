using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OmicronScanUpdateForLiantao.View;

namespace OmicronScanUpdateForLiantao
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        MainWindow ViewMainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            ViewMainWindow = new OmicronScanUpdateForLiantao.View.MainWindow();
            #region 判断系统是否已启动

            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("OmicronScanUpdateForLiantao");//获取指定的进程名   
            if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                System.Windows.MessageBox.Show("不允许重复打开软件");
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                base.OnStartup(e);
                ViewMainWindow.Show();
            }
            #endregion

        }
    }
}
