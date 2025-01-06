using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace ffm
{
    public partial class Form1 : Form
    {
        public void EditVideo(String path1, String path2)
        {
            EditVideo(path1, path2, false);
        }
        public void EditVideo(String path1, String path2, Boolean batch)
        {
            if (File.Exists(path2))
            {
                // 批量操作失败放入列表 不弹窗阻塞
                if (batch)
                {
                    listBox2.Items.Add(path1);
                }
                else
                {
                    MessageBox.Show("目标路径存在文件", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            ProcessFileDto processFile = new ProcessFileDto();
            AssignmentProcessFile(path1, path2, processFile);
            ConvertVideo(ConvertCmd(processFile));
            DeleteFileRecycleWithSetting(path1);
        }

        public void GetPic(string time)
        {
            state = null;
            GetPicBase(time, i);
            if(File.Exists(Configuration.PIC_PATH + i + ".jpg"))
            {
                pictureBox12.Image = Image.FromStream(new MemoryStream(File.ReadAllBytes(Configuration.PIC_PATH + i + ".jpg")));
            }
            DeleteFile(Configuration.PIC_PATH + i + ".jpg");
            i = (i + 1) % 20;
        }

        public void GetPicBase(string time, int index)
        {
            GetPicBase(time, Configuration.PIC_PATH + index);
        }

        public void GetPicBase(string time, String path)
        {
            DeleteFile(path + ".jpg");
            string strArg = " -ss " + time + " -i \"" + textBox1.Text + "\""
                + " -vframes 1 -q:v 2 \"" + path + ".jpg\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetPicBase));
        }

        public void GetVideo(string path)
        {
            string strArg = "-i \"" + path + "\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetVideo));
            AdjustWindow();
            trackBar21.Maximum = millisecondTotal;
            trackBar21.Value1 = 0;
            trackBar21.Value2 = millisecondTotal;
        }
        public void AdjustWindow()
        {
            if (widthFrame > heightFrame && checkBox1.Checked == true)
            {
                this.Size = new Size(570 + 220, 375);
                pictureBox12.Size = new Size(181 + 220, 296);
                /* this.Size = new Size(385, 588);
                 label6.Location = new Point(7, 334);
                 pictureBox12.Location = new Point(7, 348);
                 pictureBox12.Size = new Size(350, 200);*/
            }
            else
            {
                this.Size = new Size(570, 375);
                //label6.Location = new Point(361, 9);
                pictureBox12.Location = new Point(363, 25);
                pictureBox12.Size = new Size(181, 296);
                
            }
        }

        public void ConvertVideo(string strArg)
        {
            //textBox9.Text = textBox9.Text + generateLog(strArg);
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));

            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        //创建合并视频列表的文本
        private void CreateTextFileWithInputs()
        {
            string inputFileListPath = "filelist.txt";
            using (StreamWriter writer = new StreamWriter(inputFileListPath))
            {
                foreach (string inputVideoPath in listBox1.Items)
                {
                    writer.WriteLine("file '" + inputVideoPath + "'");
                }
            }
        }

        public void MergeVideo()
        {
            CreateTextFileWithInputs();

            string strArg = "-f concat -safe 0 -i filelist.txt -c copy \"" + textBox5.Text + "\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputMergeVideo));

            progressBar1.Value = 100;
            label8.Text = "100%";
            DeleteFile("filelist.txt");
        }

        private void OutputConvertVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    TimeSpan timeTotal = TimeSpan.Parse(total);
                    millisecondTotal = timeTotal.TotalMilliseconds;
                }

                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        progressBar1.Value = (int)(milliseconds * 100 / millisecondTotal);

                        TimeSpan timeDifference = dateTimePicker2.Value - dateTimePicker1.Value;
                        label7.Text = "进度:" + value + " ~ " + timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / millisecondTotal, 2) + "%";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常：" + ex.Message);
                    }
                }
            }
        }

        private void OutputGetVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    label7.Text = "进度:" + "0 ~ ?" + " / " + total + " ";
                    TimeSpan timeTotal = TimeSpan.Parse(total);
                    millisecondTotal = timeTotal.TotalMilliseconds;
                   /* trackBar21.Maximum = millisecondTotal;
                    trackBar21.Value1 = 0;
                    trackBar21.Value2 = millisecondTotal;*/
                    //Value1Change();
                    //Value2Change();
                }

                //通过正则表达式获取信息里面的宽度信息
                Regex regex = new Regex("(\\d{2,4})x(\\d{2,4})", RegexOptions.Compiled);
                Match m = regex.Match(output.Data);
                if (m.Success && !label6.Text.Contains("宽:"))
                {
                    widthFrame = int.Parse(m.Groups[1].Value);
                    heightFrame = int.Parse(m.Groups[2].Value);
                    label6.Text = label6.Text + " 宽:" + widthFrame + " 高:" + heightFrame;
                    widthFrameEnd = widthFrame;
                    heightFrameEnd = heightFrame;
                    textBox7.Text = widthFrame + "," + heightFrame;
                    textBox8.Text = widthFrame + "," + heightFrame;
                }
                else
                {
                }

            }
        }

        private void OutputMergeVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        progressBar1.Value = (int)(milliseconds * 100 / millisecondTotal);

                        TimeSpan timeDifference = dateTimePicker2.Value - dateTimePicker1.Value;
                        label7.Text = "进度:" + value + " ~ " + timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / millisecondTotal, 2) + "%";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常：" + ex.Message);
                    }
                }
            }
        }

        private void OutputGetPicBase(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
            }
        }
    }
}
