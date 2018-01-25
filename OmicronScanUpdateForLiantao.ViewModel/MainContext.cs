﻿using System;
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
        #endregion
        #region 变量
        string MessageStr = "";
        dialog mydialog = new dialog();
        string ParameterIniPath = @"C:\Parameter.ini";
        ThingetPLC Xinjie;
        ObservableCollection<bool> PlcIn;
        bool[] PlcOut = new bool[64];
        Scan ScanA, ScanB;
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
                default:
                    break;
            }
        }
        private void ScanActionCallback1(string str)
        {
            BarcocdeA = str;
        }
        #endregion
        #region 工作
        [Initialize]
        public void PlcRun()
        {
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
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(1000);
                            MsgText = AddMessage("PLC断线，重新连接...");
                            Xinjie.ModbusDisConnect();
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