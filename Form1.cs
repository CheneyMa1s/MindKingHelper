using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        Dm.dmsoft dm = null;
        public Form1()
        {
            InitializeComponent();

            AutoRegCom("regsvr32 -s dm.dll");
            dm = new Dm.dmsoft();

            //var hwnd = dm.FindWindowEx(0, "", "PotPlayer");
            var hwnd = dm.FindWindowEx(0, "CHWindow", null);

            if (dm.BindWindow(hwnd, "dx2", "windows", "windows", 0) == 1)
            {
                this.Text = $"绑定成功：" + hwnd;
            }
            else
            {
                this.Text = $"绑定失败";
                return;
            }

        }


        static string AutoRegCom(string strCmd)
        {
            string rInfo;


            try
            {
                Process myProcess = new Process();
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe");
                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcess.StartInfo = myProcessStartInfo;
                myProcessStartInfo.Arguments = "/c " + strCmd;
                myProcess.Start();
                var myStreamReader = myProcess.StandardOutput;
                rInfo = myStreamReader.ReadToEnd();
                myProcess.Close();
                rInfo = strCmd + "\r\n" + rInfo;
                return rInfo;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void button1_Click(object sender, EventArgs args)
        {
            Console.Clear();

            dm.Capture(0, 200, 344, 730, Application.StartupPath + "\\1.bmp");

            using (var web = new System.Net.WebClient() { Encoding = Encoding.UTF8 })
            {
                web.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                string filename = Guid.NewGuid().ToString("N") + ".jpg";

                FileStream fs = new FileStream(Application.StartupPath + "\\1.bmp", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] bys = new byte[(int)fs.Length];
                fs.Read(bys, 0, (int)fs.Length);

                var bitmap = new Bitmap(fs);
                Graphics.FromImage(bitmap).FillRectangles(Brushes.Black, new RectangleF[] { new RectangleF(0, 250, 70, 300), new RectangleF(344 - 70, 250, 70, 300) });
                var fileName = Guid.NewGuid().ToString("N") + ".bmp";
                bitmap.Save(Application.StartupPath + $"/Tmp/{fileName}", ImageFormat.Jpeg);
                //		System.IO.File.WriteAllBytes("C:\\" + DateTime.Now.Ticks + ".bmp",bys);

                string str = Convert.ToBase64String(System.IO.File.ReadAllBytes(Application.StartupPath + $"/Tmp/{fileName}"));
                str = System.Web.HttpUtility.UrlEncode(str);
                string url = "https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token=24.894dd3281f8e9f0f7ab438f68e71327d.2592000.1518775914.282335-10707492";
                var res = web.UploadString(url, "image=" + str);



                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(res);
                List<string> arr = new List<string>();
                List<string> ans = new List<string>();
                foreach (dynamic item in obj.words_result)
                {
                    arr.Add(item.words.ToString());
                }

                if (arr.Count <= 4)
                {
                    Console.WriteLine("GG");
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    ans.Insert(0, arr.Last());
                    arr.Remove(arr.Last());
                }

                string answer = "";
                for (int i = 0; i < arr.Count; i++)
                {
                    answer += arr[i];
                }

                var pointDic = new Dictionary<string, int>();
                var timer = 0;

                Console.WriteLine("开始寻找指数分析：");
                foreach (var item in ans)
                {
                    using (var net = new System.Net.WebClient() { Encoding = Encoding.UTF8 })
                    {
                        net.DownloadStringCompleted += (s, e) =>
                        {
                            var html = e.Result;
                            var result = "";
                            timer++;
                            result = new Regex("(?<=相关结果约).*?(?=个)").Match(html).ToString().Replace(",", "");
                            try
                            {
                                pointDic.Add(e.UserState + "", int.Parse(result));
                            }
                            catch (Exception ex)
                            {

                            }
                            if (timer == 4)
                            {
                                Console.WriteLine("指数搜索结果");
                                Console.WriteLine("=================");
                                foreach (var p in pointDic.OrderByDescending(i => i.Value))
                                {
                                    Console.WriteLine($"{p.Key}   {p.Value}");
                                }
                                Console.WriteLine("=================");
                            }
                            //if (timer >= 4)
                            //{
                            //    foreach (var point in pointDic)
                            //    {
                            //        Console.WriteLine(Console.WriteLine(""));
                            //    }
                            //    try
                            //    {
                            //        pointDic.OrderByDescending(i => i.Value).First().Key.Dump("指数推荐答案");
                            //    }
                            //    catch
                            //    {
                            //    }
                            //    Over();
                            //}
                        };
                        string urlstring = "http://www.baidu.com/s?wd=" + System.Web.HttpUtility.UrlEncode(answer + " intitle:" + item);
                        net.DownloadStringAsync(new Uri(urlstring), item);
                    }
                }

                using (var net = new System.Net.WebClient() { Encoding = Encoding.UTF8 })
                {
                    net.DownloadStringCompleted += (s, e) =>
                    {
                        var html = e.Result;
                        Console.WriteLine("首页符合结果：");
                        var ps = ans.Select(i => new { Key = i, Point = new Regex(i).Matches(html).Count });
                        Console.WriteLine("===================");
                        Console.WriteLine("推荐答案：" + ps.OrderByDescending(i => i.Point).First().Key);
                        foreach (var p in ps)
                        {
                            Console.WriteLine($"{p.Key}   {p.Point}");
                        }
                        Console.WriteLine("===================");

                    };

                    string urlstring = "http://www.baidu.com/s?wd=" + System.Web.HttpUtility.UrlEncode(answer);
                    net.DownloadStringAsync(new Uri(urlstring));
                }
            }
        }
    }
}
