using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingLibrary.hjb;
using BingLibrary.hjb.Intercepts;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using SxjLibrary;
using BingLibrary.hjb.PLC;
using System.Windows.Threading;
using System.IO;
using System.Data;

namespace OmicronScanUpdateForLiantao.ViewModel
{
    [BingAutoNotify]
    public class MainContext : DataSource
    {
        #region 属性
        public virtual bool PLCConnect { set; get; }//PLC连接状态
        public virtual string MsgText { set; get; }//打印信息
        public virtual string BarcocdeA { set; get; }
        public virtual string BarcocdeB { set; get; }
        public virtual ObservableCollection<RecordItem> RecordCollection { set; get; }
        public virtual string HomePageVisibility { set; get; }
        public virtual string RecordPageVisibility { set; get; }
        public virtual string ParameterPageVisibility { set; get; }
        public virtual int TrigerTimes { set; get; }
        public virtual int ScanTimes { set; get; }
        public virtual int UpdateTimes { set; get; }
        public virtual string JiTaiHao { set; get; }
        public virtual string PLCPortCom { set; get; }
        public virtual string ScanAPortCom { set; get; }
        public virtual string ScanBPortCom { set; get; }
        public virtual double RotalAngle { set; get; }
        #endregion
        #region 变量
        string MessageStr = "";
        dialog mydialog = new dialog();
        string ParameterIniPath = @"D:\Parameter.ini";
        ThingetPLC Xinjie;
        ObservableCollection<bool> PlcIn;
        bool[] PlcOut = new bool[8];
        Scan ScanA, ScanB;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        string LastCleanRecordFlag;
        List<RecordItem> recordItemList = new List<RecordItem>();
        object LockObject = new object();
        int rol = 0;
        double dd8170 = 0, dd4208 = 0;
        string Abarcode, Bbarcode;
        bool Abarcode_f, Bbarcode_f, Abarcode_s, Bbarcode_s;
        #endregion
        #region 构造函数
        public MainContext()
        {
            RecordCollection = new ObservableCollection<RecordItem>();            
        }
        #endregion
        #region 软件打开关闭
        public void AppLoaded()
        {
            Xinjie = new ThingetPLC();
            ScanA = new Scan();
            ScanB = new Scan();
            ReadParameter();
            ScanA.ini(ScanAPortCom);
            ScanA.Connect();
            ScanB.ini(ScanBPortCom);
            ScanB.Connect();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerAction);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            ReadRecordFromFile();
            dispatcherTimer.Start();
        }
        #endregion
        #region 界面功能函数
        /// <summary>
        /// 打印窗口字符处理函数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string AddMessage(string str)
        {
            string[] s = MessageStr.Split('\n');
            if (s.Length > 1000)
            {
                MessageStr = "";
            }
            if (MessageStr != "")
            {
                MessageStr += "\n";
            }
            MessageStr += System.DateTime.Now.ToString() + " " + str;
            return MessageStr;
        }
        public async void ChoosePageAction(object p)
        {
            switch (p.ToString())
            {
                case "0":
                    HomePageVisibility = "Visible";
                    RecordPageVisibility = "Collapsed";
                    ParameterPageVisibility = "Collapsed";
                    break;
                case "1":
                    HomePageVisibility = "Collapsed";
                    RecordPageVisibility = "Visible";
                    ParameterPageVisibility = "Collapsed";
                    break;
                case "2":
                    List<string> r;
                    r = await mydialog.showlogin();
                    if (r[1] == Inifile.INIGetStringValue(ParameterIniPath, "Password", "Psw", "QWER7788")) 
                    {
                        HomePageVisibility = "Collapsed";
                        RecordPageVisibility = "Collapsed";
                        ParameterPageVisibility = "Visible";
                    }
                    else
                    {
                        HomePageVisibility = "Visible";
                        RecordPageVisibility = "Collapsed";
                        ParameterPageVisibility = "Collapsed";
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region 功能函数
        private void ReadParameter()
        {
            JiTaiHao = Inifile.INIGetStringValue(ParameterIniPath,"Text", "JiTaiHao","01");
            BarcocdeA = Inifile.INIGetStringValue(ParameterIniPath, "Text", "BarcocdeA", "ABC");
            BarcocdeB = Inifile.INIGetStringValue(ParameterIniPath, "Text", "BarcocdeB", "DEF");
            PLCPortCom = Inifile.INIGetStringValue(ParameterIniPath, "Text", "PLCPortCom", "COM1");
            ScanAPortCom = Inifile.INIGetStringValue(ParameterIniPath, "Text", "ScanAPortCom", "COM1");
            ScanBPortCom = Inifile.INIGetStringValue(ParameterIniPath, "Text", "ScanBPortCom", "COM1");
            LastCleanRecordFlag = Inifile.INIGetStringValue(ParameterIniPath, "Record", "LastCleanRecordFlag", "123");
            TrigerTimes = int.Parse(Inifile.INIGetStringValue(ParameterIniPath, "Times", "TrigerTimes", "0"));
            ScanTimes = int.Parse(Inifile.INIGetStringValue(ParameterIniPath, "Times", "ScanTimes", "0"));
            UpdateTimes = int.Parse(Inifile.INIGetStringValue(ParameterIniPath, "Times", "UpdateTimes", "0"));
        }
        public void SaveParameterAction()
        {
            Inifile.INIWriteValue(ParameterIniPath, "Text", "PLCPortCom", PLCPortCom);
            Inifile.INIWriteValue(ParameterIniPath, "Text", "ScanAPortCom", ScanAPortCom);
            Inifile.INIWriteValue(ParameterIniPath, "Text", "ScanBPortCom", ScanBPortCom);
        }
        public void ScanAction(object p)
        {
            switch (p.ToString())
            {
                case "0":
                    ScanA.GetBarCode(ScanActionCallback1);
                    break;
                case "1":
                    ScanB.GetBarCode(ScanActionCallback2);
                    break;
                default:
                    break;
            }
        }
        private void ScanActionCallback1(string str)
        {
            BarcocdeA = str;
        }
        private void ScanActionCallback2(string str)
        {
            BarcocdeB = str;
        }
        private string GetBanciDate()
        {
            string rtstr = "";
            if (DateTime.Now.Hour >=0 && DateTime.Now.Hour < 8)
            {
                rtstr = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            else
            {
                rtstr = DateTime.Now.ToString("yyyy-MM-dd");
            }
            return rtstr;
        }
        private string GetBanci()
        {
            string rtstr = "";
            if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
            {
                rtstr = "D";
            }
            else
            {
                rtstr = "N";
            }
            return rtstr;
        }
        private void DispatcherTimerAction(Object sender, EventArgs e)
        {
            if (!File.Exists(@"D:\Maintain.csv"))
            {
                string[] heads = { "时间", "机台号", "触发次数", "扫码次数", "上传次数" };
                Csvfile.AddNewLine(@"D:\Maintain.csv", heads);
            }
            if (LastCleanRecordFlag != GetBanciDate() + GetBanci())
            {
                LastCleanRecordFlag = GetBanciDate() + GetBanci();
                Inifile.INIWriteValue(ParameterIniPath, "Record", "LastCleanRecordFlag", LastCleanRecordFlag);
                if (!Directory.Exists("D:\\" + LastCleanRecordFlag))
                {
                    Directory.CreateDirectory("D:\\" + LastCleanRecordFlag);
                }
                string[] count = { DateTime.Now.ToString(), JiTaiHao, TrigerTimes.ToString(), ScanTimes.ToString(), UpdateTimes.ToString() };
                Csvfile.AddNewLine(@"D:\Maintain.csv", count);
                TrigerTimes = 0;
                ScanTimes = 0;
                UpdateTimes = 0;
                Inifile.INIWriteValue(ParameterIniPath, "Times", "TrigerTimes", TrigerTimes.ToString());
                Inifile.INIWriteValue(ParameterIniPath, "Times", "ScanTimes", ScanTimes.ToString());
                Inifile.INIWriteValue(ParameterIniPath, "Times", "UpdateTimes", UpdateTimes.ToString());
                MsgText = AddMessage("记录清空");
            }
            if (recordItemList.Count > 0)
            {
                lock (LockObject)
                {
                    foreach (RecordItem item in recordItemList)
                    {
                        RecordCollection.Add(item);
                    }
                    recordItemList.Clear();
                }
            }
            
        }
        private void ReadRecordFromFile()
        {
            if (File.Exists("D:\\" + GetBanciDate()+ GetBanci() + "\\" + GetBanciDate() + GetBanci() + ".csv"))
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("日期", typeof(string));
                dt.Columns.Add("班次", typeof(string));
                dt.Columns.Add("机台号", typeof(string));
                dt.Columns.Add("机台穴号", typeof(string));
                dt.Columns.Add("产品barcode", typeof(string));
                DataTable dt1 = Csvfile.GetFromCsv("D:\\" + GetBanciDate() + GetBanci() + "\\" + GetBanciDate() + GetBanci() + ".csv", 1, dt);
                if (dt1.Rows.Count > 0)
                {
                    foreach (DataRow item in dt1.Rows)
                    {
                        RecordItem ri = new RecordItem();
                        ri.日期 = item["日期"].ToString();
                        ri.班次 = item["班次"].ToString();
                        ri.机台号 = item["机台号"].ToString();
                        ri.机台穴号 = item["机台穴号"].ToString();
                        ri.产品barcode = item["产品barcode"].ToString();
                        lock (LockObject)
                        {
                            recordItemList.Add(ri);
                        }
                    }
                }
            }
            else
            {
                MsgText = AddMessage("本地记录不存在");
            }
        }
        #endregion
        #region 工作
        [Initialize]
        public void PlcRun()
        {
            bool first = true;
            bool scanFlag = false;
            Random rd = new Random();
            while (true)
            {
                System.Threading.Thread.Sleep(10);
                if (Xinjie == null)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    try
                    {
                        PLCConnect = Xinjie.ReadM(24576);
                        if (PLCConnect)
                        {
                            PlcIn = Xinjie.ReadMultiMCoil(1500);
                            Xinjie.WritMultiMCoil(1800, PlcOut);
                            Xinjie.WriteW(150, rd.Next(0, 999).ToString());
                            dd8170 = Xinjie.ReadD(16554);
                            dd4208 = Xinjie.ReadD(4208);
                            RotalAngle = (dd8170 - dd4208) / 91776 * 360;

                            //扫码
                            if (scanFlag != PlcIn[0])
                            {
                                scanFlag = PlcIn[0];
                                if (scanFlag)
                                {
                                    Abarcode_f = false;
                                    Bbarcode_f = false;
                                    PlcOut[0] = false;
                                    PlcOut[1] = false;
                                    PlcOut[2] = false;
                                    Xinjie.WritMultiMCoil(1800, PlcOut);
                                    ScanA.GetBarCode(ScanActionCallback1);
                                    ScanB.GetBarCode(ScanActionCallback2);
                                    TrigerTimes++;
                                    Inifile.INIWriteValue(ParameterIniPath, "Times", "TrigerTimes", TrigerTimes.ToString());
                                }
                                else
                                {
                                    PlcOut[0] = false;
                                }
                            }
                            if (Abarcode_f && Bbarcode_f)
                            {
                                Abarcode_f = false;
                                Bbarcode_f = false;
                                PlcOut[0] = true;
                                PlcOut[1] = Abarcode_s;
                                PlcOut[2] = Bbarcode_s;
                                if (Abarcode_s && Bbarcode_s)
                                {
                                    ScanTimes++;
                                    Inifile.INIWriteValue(ParameterIniPath, "Times", "ScanTimes", ScanTimes.ToString());
                                }
                                RecordItem recordItem = new RecordItem();
                                recordItem.日期 = GetBanciDate();
                                recordItem.班次 = GetBanci();
                                recordItem.机台号 = JiTaiHao;
                                recordItem.产品barcode = Abarcode;
                                recordItem.机台穴号 = Bbarcode;
                                lock (LockObject)
                                {
                                    recordItemList.Add(recordItem);
                                }
                                if (Directory.Exists("D:\\" + GetBanciDate() + GetBanci()))
                                {
                                    string filename = "D:\\" + GetBanciDate() + GetBanci() + "\\" + GetBanciDate() + GetBanci() + ".csv";
                                    if (File.Exists(filename))
                                    {
                                        string[] heads = { "日期", "班次", "机台号", "机台穴号", "产品barcode" };
                                        Csvfile.AddNewLine(filename, heads);
                                    }
                                    string[] count = { recordItem.日期, recordItem.班次, recordItem.机台号, recordItem.机台穴号, recordItem.产品barcode };
                                    Csvfile.AddNewLine(filename, count);
                                }
                            }
                        }
                        else
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                MsgText = AddMessage("PLC断线，重新连接...");
                            }
                            Xinjie.ModbusDisConnect();
                            System.Threading.Thread.Sleep(1000);
                            Xinjie.ModbusInit(PLCPortCom, 19200, System.IO.Ports.Parity.Even, 8, System.IO.Ports.StopBits.One);
                            Xinjie.ModbusConnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        MsgText = AddMessage(ex.Message);
                        PLCConnect = false;
                    }
                    
                }
            }
        }
        private void PLCScanCallback1(string str)
        {
            BarcocdeA = Abarcode = str;
            if (str != "Error")
            {
                Abarcode_s = true;
                MsgText = AddMessage("扫码A成功 " + str);
            }
            else
            {
                Abarcode_s = false;
                MsgText = AddMessage("扫码A失败 " + str);
            }
            Abarcode_f = true;
        }
        private void PLCScanCallback2(string str)
        {
            BarcocdeB = Bbarcode = str;
            if (str != "Error")
            {
                Bbarcode_s = true;
                MsgText = AddMessage("扫码B成功 " + str);
            }
            else
            {
                Bbarcode_s = false;
                MsgText = AddMessage("扫码B失败 " + str);
            }
            Bbarcode_f = true;
        }
        #endregion
    }
    class VMManager
    {
        [Export(MEF.Contracts.Data)]
        [ExportMetadata(MEF.Key, "md")]
        MainContext md = MainContext.New<MainContext>();
    }
    public class RecordItem
    {
        public string 日期 { set; get; }
        public string 班次 { set; get; }
        public string 机台号 { set; get; }
        public string 机台穴号 { set; get; }
        public string 产品barcode { set; get; }
    }
}
