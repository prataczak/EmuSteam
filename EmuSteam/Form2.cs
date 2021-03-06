﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Web;
using System.Net;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace EmuSteam
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private string retroarchDir = Application.StartupPath + @"\retroarch\";
        private string retroCores = Application.StartupPath + @"\retroarch\cores";

        private void getRA(string architecture)
        {
            progressbarText.Visible = true;
            progressBar1.Value = 0;
            if (!backgroundWorker1.IsBusy)
                backgroundWorker1.RunWorkerAsync(architecture);
            statusLabel.Text = "STATUS: Download files...";
            statusLabel.Visible = true;
            progressBar1.Visible = true;
            if (architecture.Contains("core") == true)
                button4.Enabled = false;
            else
                button3.Enabled = false;
        }

        /*private void button1_Click(object sender, EventArgs e) //get 32 bit 
        {
            //((Form1)Owner).getRetroArch();
            //this.Close();
            getRA("32");
        }*/

        static String BytesToString(double byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs((long)byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        static string DisplayPercentage(double ratio)
        {
            return string.Format("{0.0%}", ratio);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void ExtractFileToDirectory(string zipFileName, string outputDirectory)
        {
            /*if (statusLabel.InvokeRequired)
                statusLabel.Invoke(new MethodInvoker(delegate
                {
                    statusLabel.Text = "STATUS: Unzipping files. Please wait...";
                }));*/

            Directory.CreateDirectory(outputDirectory);

            if (zipFileName.Contains(".7z"))
            {
                string tmpFile = outputDirectory + @"\tmp.7z";
                //MessageBox.Show(tmpFile);
                ProcessStartInfo zipper = new ProcessStartInfo("7z.exe");
                zipper.Arguments = string.Format("x {0} -o{1}", tmpFile, outputDirectory);
                //zipper.RedirectStandardInput = true;
                //zipper.UseShellExecute = false;
                //zipper.CreateNoWindow = true;
                //zipper.WindowStyle = ProcessWindowStyle.Hidden;
                Process process = Process.Start(zipper);
            } else {
                MessageBox.Show("normal zip");
                FileStream fs = File.OpenRead(zipFileName);
                ZipFile zip = ZipFile.Read(fs);

                foreach (ZipEntry e in zip)
                {
                    e.Extract(outputDirectory, ExtractExistingFileAction.OverwriteSilently);
                }
                fs.Close();
                File.Delete(zipFileName);
            }
        }



        public static string GetPageAsString(string url)
        {
            string html = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string retroarchDir = Application.StartupPath + @"\retroarch";
            if (!Directory.Exists(retroarchDir))
                Directory.CreateDirectory(retroarchDir);
            string dynamicURL = e.Argument as string;
            string realURL = HttpUtility.HtmlDecode("http://newagesoldier.com/myfiles/xml/emusteam/getra.php?v=" + e.Argument);

            string coreDir = Application.StartupPath + @"\retroarch\cores";
            string sFilePathToWriteFileTo = "";

            string sUrlToReadFileFrom = realURL;
            if (dynamicURL.Contains("core") == true)
                sFilePathToWriteFileTo = retroarchDir + @"\tmp.zip";
            else
                sFilePathToWriteFileTo = retroarchDir + @"\tmp.7z";

            Uri url = new Uri(sUrlToReadFileFrom);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();
            long iSize = response.ContentLength;
            Int64 iRunningByteTotal = 0;

            using (WebClient client = new WebClient())
            {
                using (Stream streamRemote = client.OpenRead(new Uri(sUrlToReadFileFrom)))
                {
                    using (Stream streamLocal = new FileStream(sFilePathToWriteFileTo, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        int iByteSize = 0;
                        byte[] byteBuffer = new byte[iSize];
                        while ((iByteSize = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                        {
                            streamLocal.Write(byteBuffer, 0, iByteSize);
                            iRunningByteTotal += iByteSize;

                            if (backgroundWorker1.CancellationPending == true)
                            {
                                e.Cancel = true;
                                break;
                            }

                            double dIndex = (double)(iRunningByteTotal);
                            double dTotal = (double)byteBuffer.Length;
                            double dProgressPercentage = (dIndex / dTotal);
                            int iProgressPercentage = (int)(dProgressPercentage * 100);
                            if (dIndex > 0 && dTotal > 0)
                            {
                                progressbarText.Invoke(new MethodInvoker(delegate
                                {
                                    progressbarText.Text = BytesToString(dIndex).ToString() + "/" + BytesToString(dTotal).ToString() + " (" + iProgressPercentage.ToString() + "%)";
                                }));
                                backgroundWorker1.ReportProgress(iProgressPercentage);
                            }
                        }
                        streamLocal.Close();
                    }
                    streamRemote.Close();
                }
            }

            if (dynamicURL.Contains("core") == true)
                ExtractFileToDirectory(sFilePathToWriteFileTo, coreDir);
            else
                ExtractFileToDirectory(sFilePathToWriteFileTo, retroarchDir);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void retroArchFolderCheck()
        {
            if (Directory.Exists(retroarchDir))
            {
                button3.Enabled = false; //get btn
                button4.Enabled = true; //you can now get retroarch cores
                button1.Enabled = true; //delete btn
            }
            else
            {
                button3.Enabled = true; //get btn
                button4.Enabled = false; //cant get cores without retroarch dir
                button1.Enabled = false; //delete btn
                progressBar1.Visible = false;
                statusLabel.Visible = false;
                textBox1.Text = "You do not have Retroarch in the proper folder. Would you like this program to get the proper files?";
            }

            if (Directory.Exists(retroCores))
            {
                button4.Enabled = false; //get btn
                button5.Enabled = true; //delete btn
                textBox1.Text = "You appear to have Retroarch and game cores. You are ready to play. Enjoy!";
            }
            else
            {
                button5.Enabled = false; //delete btn
                progressBar1.Visible = false;
                statusLabel.Visible = false;
                if (Directory.Exists(retroarchDir))
                    textBox1.Text = "You appear to have retroarch but not game cores. You need the game cores to play the games!";
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusLabel.Text = "STATUS: COMPLETE!";
            retroArchFolderCheck();
            progressbarText.Visible = false;
            retroArchFolderCheck(); //check again
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //VERSION TEST
            string stringvar = GetPageAsString("http://buildbot.libretro.com/stable/");
            foreach (Match match in Regex.Matches(stringvar, "<td class=\'fb-n\'><a href=\'(.*?)\'>", RegexOptions.Singleline))
            {
                if (match.Value.Contains("..") || match.Value.Contains("archive"))
                    continue;
                string ver = match.Value.Replace("<td class='fb-n'><a href='/stable/", "");
                ver = ver.Replace("/'>","");
                MessageBox.Show(ver);
            }
            //

            retroArchFolderCheck();
            if (Owner != null)
                Location = new Point(Owner.Location.X + Owner.Width / 2 - Width / 2,
                    Owner.Location.Y + Owner.Height / 2 - Height / 2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            getRA("64");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (Directory.Exists(@".\retroarch"))
                Directory.Delete(retroarchDir, true);
            retroArchFolderCheck();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; //don't delete retroarch while we are downloading!
            string coreDir = Application.StartupPath + @"\retroarch\cores";
            if (!Directory.Exists(coreDir))
                Directory.CreateDirectory(coreDir);
            getRA("cores64");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Directory.Delete(retroCores, true);
            retroArchFolderCheck();
        }
    }
}
