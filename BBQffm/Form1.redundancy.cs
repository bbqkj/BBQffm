using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ffm
{
    public partial class Form1 : Form
    {
        public void AssignmentComboBox1()
        {
            ArrayList comboBoxList = new ArrayList();
            comboBoxList.Add(new DictionaryEntry("无", "-1"));
            comboBoxList.Add(new DictionaryEntry("逆时针旋转90°后再水平翻转", "0"));
            comboBoxList.Add(new DictionaryEntry("顺时针旋转90°", "1"));
            comboBoxList.Add(new DictionaryEntry("逆时针旋转90°", "2"));
            comboBoxList.Add(new DictionaryEntry("顺时针旋转90°后再水平翻转°", "3"));
            comboBoxList.Add(new DictionaryEntry("水平翻转", "4"));
            comboBoxList.Add(new DictionaryEntry("垂直翻转", "5"));
            comboBox1.DataSource = comboBoxList;
            comboBox1.DisplayMember = "Key";
            comboBox1.ValueMember = "Value";
        }

        // 初始化进度条
        public void IniProgressBar()
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }

        public void IniCache()
        {
            if(GetSettings("CHECKBOX1") != null)
            {
                checkBox1.Checked = Convert.ToBoolean(GetSettings("CHECKBOX1"));
            }

            if (GetSettings("COMBOBOX2") != null)
            {
                comboBox2.DataSource = LoadCombobox2();
            }

            if (GetSettings("CHECKBOX2") != null)
            {
                checkBox2.Checked = Convert.ToBoolean(GetSettings("CHECKBOX2"));
            }

            if (GetSettings("CHECKBOX3") != null)
            {
                checkBox3.Checked = Convert.ToBoolean(GetSettings("CHECKBOX3"));
            }

            if (GetSettings("TEXTBOX10") != null)
            {
                Configuration.LOG_PATH = GetSettings("TEXTBOX10").ToString();
                
            }
            textBox10.Text = Configuration.LOG_PATH;

            if (GetSettings("TEXTBOX11") != null)
            {
                Configuration.PIC_PATH = GetSettings("TEXTBOX11").ToString();

            }
            textBox11.Text = Configuration.PIC_PATH;
        }

        ArrayList LoadCombobox2()
        {
            ArrayList comboBoxList;
            String combobox2Str = (String)GetSettings("COMBOBOX2");
            if (combobox2Str == null || "".Equals(combobox2Str))
            {
                comboBoxList = new ArrayList();
            }
            else
            {
                comboBoxList = JsonConvert.DeserializeObject<ArrayList>(combobox2Str);
            }
            return comboBoxList;
        }

        public void AssignmentDictionary(Dictionary<string, string> dic)
        {
            dic.Add("processFile", textBox1.Text);
            dic.Add("targetFile", textBox2.Text);
            dic.Add("vf", "transpose=" + comboBox1.SelectedValue);
            dic.Add("crf", numericUpDown1.Value.ToString());
            dic.Add("beginTime", dateTimePicker1.Text);
            dic.Add("endTime", dateTimePicker2.Text);
        }

        void Reset()
        {
            // 帧坐标 即视频宽高
            widthFrame = 0;
            heightFrame = 0;

            // 起点帧坐标
            widthFrameStart = 0;
            heightFrameStart = 0;

            // 终点帧坐标
            widthFrameEnd = 0;
            heightFrameEnd = 0;

            // 剪辑后的宽高
            cutWidthFrame = 0;
            cutHeightFrame = 0;

            progressBar1.Value = 0;
            label8.Text = "0%";
            textBox6.Text = "0,0";

            numericUpDown2.Value = -1;

            listBox3.Items.Clear();
            this.listBox3.Visible = false;
            this.button19.Visible = false;
        }

        public void AssignmentProcessFile(String path1, String path2, ProcessFileDto processFile)
        {
            processFile.processFile = "\"" + path1 + "\"";
            processFile.targetFile = "\"" + path2 + "\"";

            if ("-1".Equals(comboBox1.SelectedValue.ToString()) || "6".Equals(comboBox1.SelectedValue.ToString()))
            {
                processFile.vf = null;
            }
            else if ("4".Equals(comboBox1.SelectedValue.ToString()))
            {
                processFile.vf = "hflip";
            }
            else if ("5".Equals(comboBox1.SelectedValue.ToString()))
            {
                processFile.vf = "vflip";
            }
            else
            {
                processFile.vf = "transpose=" + comboBox1.SelectedValue.ToString();
            }

            processFile.crf = numericUpDown1.Value.ToString();
            processFile.beginTime = textBox3.Text;
            processFile.endTime = textBox4.Text;
        }

        public String ConvertCmd(ProcessFileDto processFile)
        {
            string strArg = "-i " + processFile.processFile + " ";

            if (processFile.vf != null && !"".Equals(processFile.vf))
            {
                strArg += "-vf " + processFile.vf + " ";
            }
            else if (cutWidthFrame > 0 && cutHeightFrame > 0 && !(cutWidthFrame == widthFrame && cutHeightFrame == heightFrame))
            {
                strArg += "-vf crop=" + cutWidthFrame + ":" + cutHeightFrame + ":" + widthFrameStart + ":" + heightFrameStart + " ";
            }
            strArg += "-vcodec h264 ";
            if (processFile.crf != null && !"".Equals(processFile.crf))
            {
                strArg += "-crf " + processFile.crf + " ";
            }
            if (processFile.beginTime != null && !"".Equals(processFile.beginTime))
            {
                strArg += "-acodec copy -ss " + processFile.beginTime + " -to " + processFile.endTime + " ";
            }
            strArg += processFile.targetFile + " ";

            return strArg;
        }

        public void CalculationArea(int X , int Y, bool isStart)
        {
            int widthFrameTmp = widthFrame * X / pictureBox12.newWidth;
            int heightFrameTmp = heightFrame * Y / pictureBox12.newHeight;
            if (widthFrameTmp > widthFrame)
            {
                widthFrameTmp = widthFrame;
            }
            if (heightFrameTmp > heightFrame)
            {
                heightFrameTmp = heightFrame;
            }
            if (isStart)
            {
                widthFrameStart = widthFrameTmp;
                heightFrameStart = heightFrameTmp;
                textBox6.Text = widthFrameStart + "," + heightFrameStart;
            } 
            else
            {
                widthFrameEnd = widthFrameTmp;
                heightFrameEnd = heightFrameTmp;
                textBox7.Text = widthFrameEnd + "," + heightFrameEnd;
            }

            cutWidthFrame = widthFrameEnd - widthFrameStart;
            cutHeightFrame = heightFrameEnd - heightFrameStart;
            textBox8.Text = cutWidthFrame + "," + cutHeightFrame;

            DrawGraphics();
        }

        public void DrawGraphics()
        {
            Graphics g = pictureBox12.CreateGraphics();
            Pen pen = new Pen(Color.Red, 2);
            pictureBox12.Refresh‌();
            g.DrawRectangle(pen, new Rectangle(widthFrameStart * pictureBox12.newWidth / widthFrame, heightFrameStart * pictureBox12.newHeight / heightFrame, cutWidthFrame * pictureBox12.newWidth / widthFrame,
                cutHeightFrame * pictureBox12.newWidth / widthFrame));
            g.Dispose();
        }

        public void ShowFileSize(String path)
        {
            if (File.Exists(path))
            {
                label6.Text = "大小：" + GetFileSize(new FileInfo(path).Length);
            }
        }

        public bool ExistsFile()
        {
            if (File.Exists(textBox2.Text))
            {
                MessageBox.Show("目标路径存在文件", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                numericUpDown2.Value++;
                return true;
            }
            return false;
        }

        void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            this.pictureBox12.Width += e.Delta / 10;
            this.pictureBox12.Height += e.Delta / 10;
            this.Width += e.Delta / 10;
            this.Height += e.Delta / 10;
        }
    }
}
