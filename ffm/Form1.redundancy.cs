using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

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

            if (GetSettings("CHECKBOX4") != null)
            {
                checkBox4.Checked = Convert.ToBoolean(GetSettings("CHECKBOX4"));
            }

            if (GetSettings("CHECKBOX5") != null)
            {
                checkBox5.Checked = Convert.ToBoolean(GetSettings("CHECKBOX5"));
            }

            if (GetSettings("CHECKBOX6") != null)
            {
                checkBox6.Checked = Convert.ToBoolean(GetSettings("CHECKBOX6"));
            }

            if (GetSettings("CHECKBOX7") != null)
            {
                checkBox7.Checked = Convert.ToBoolean(GetSettings("CHECKBOX7"));
            }

            if (GetSettings("CHECKBOX9") != null)
            {
                checkBox9.Checked = Convert.ToBoolean(GetSettings("CHECKBOX9"));
            }

            if (GetSettings("CHECKBOX10") != null)
            {
                checkBox10.Checked = Convert.ToBoolean(GetSettings("CHECKBOX10"));
            }

            if (GetSettings("CHECKBOX11") != null)
            {
                checkBox11.Checked = Convert.ToBoolean(GetSettings("CHECKBOX11"));
            }

            if (GetSettings("RADIOBUTTON1") != null)
            {
                radioButton1.Checked = Convert.ToBoolean(GetSettings("RADIOBUTTON1"));
            }

            if (GetSettings("RADIOBUTTON2") != null)
            {
                radioButton2.Checked = Convert.ToBoolean(GetSettings("RADIOBUTTON2"));
            }

            if (GetSettings("RADIOBUTTON3") != null)
            {
                radioButton3.Checked = Convert.ToBoolean(GetSettings("RADIOBUTTON3"));
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

        private string BuildCopySegmentCommand(string input, double startSec, double endSec, string output)
        {
            // 使用快速剪辑的流复制模式，-ss 在前
            return $"-ss {startSec.ToString("F9", CultureInfo.InvariantCulture)} -to {endSec.ToString("F9", CultureInfo.InvariantCulture)} -i \"{input}\" -c copy \"{output}\"";
        }

        private string BuildReencodeSegmentCommand(string input, double startSec, double endSec, string output, ProcessFileDto processFile)
        {
            // 时间参数放在 -i 之后以实现精确 seek
            string strArg = $"-i \"{input}\" -ss {startSec.ToString("F9", CultureInfo.InvariantCulture)} -to {endSec.ToString("F9", CultureInfo.InvariantCulture)} ";

            // 编码参数（参考原代码中的普通剪辑部分）
            if (checkBox11.Checked)
                strArg += "-c:v h264_nvenc ";
            else
                strArg += "-c:v libx264 ";
            strArg += "-c:a aac "; // 重编码音频

            // 添加 CRF 等参数
            if (!checkBox9.Checked && processFile.crf != null && !"".Equals(processFile.crf) && !"-1".Equals(processFile.crf))
            {
                if (checkBox11.Checked)
                    strArg += "-cq " + processFile.crf + " ";
                else
                    strArg += "-crf " + processFile.crf + " ";
            }

            // 添加滤镜（裁剪、缩放等）
            if (!checkBox9.Checked && processFile.vf != null && !"".Equals(processFile.vf))
                strArg += "-vf " + processFile.vf + " ";
            else if (!checkBox9.Checked && cutWidthFrame > 0 && cutHeightFrame > 0 && !(cutWidthFrame == widthFrame && cutHeightFrame == heightFrame))
                strArg += "-vf crop=" + cutWidthFrame + ":" + cutHeightFrame + ":" + widthFrameStart + ":" + heightFrameStart + " ";

            strArg += output + " ";
            return strArg;
        }
        public String ConvertCmd(ProcessFileDto processFile)
        {
            Boolean isIamge = IsAppointFileByFileName(processFile.processFile, Configuration.IMAGE_EXTENSIONS);

            Console.WriteLine("bbqq " + isIamge + " d " + processFile.processFile);
            string strArg = "";

            if (checkBox9.Checked)
            {
                strArg += "-allowed_extensions ALL ";
            }

            if (checkBox10.Checked && !isIamge && processFile.beginTime != null && !"".Equals(processFile.beginTime))
            {
                strArg += "-ss " + processFile.beginTime + " -to " + processFile.endTime + " ";
            }

            strArg += "-i " + processFile.processFile + " ";


            if (!checkBox9.Checked && !isIamge && processFile.vf != null && !"".Equals(processFile.vf))
            {
                strArg += "-vf " + processFile.vf + " ";
            }
            else if (!checkBox9.Checked && !isIamge && cutWidthFrame > 0 && cutHeightFrame > 0 && !(cutWidthFrame == widthFrame && cutHeightFrame == heightFrame))
            {
                strArg += "-vf crop=" + cutWidthFrame + ":" + cutHeightFrame + ":" + widthFrameStart + ":" + heightFrameStart + " ";
            }

            // old

            if (!checkBox9.Checked && processFile.crf != null && !"".Equals(processFile.crf) && !"-1".Equals(processFile.crf))
            {
                if (isIamge)
                {
                    strArg += "-q:v " + processFile.crf + " ";
                }
                else if (checkBox11.Checked)
                {
                    strArg += "-cq " + processFile.crf + " ";
                }
                else 
                {
                    strArg += "-crf " + processFile.crf + " ";
                }
            }

            if (isIamge && !("").Equals(textBox7.Text))
            {
                string wide = textBox7.Text.Split(',')[0];
                Console.WriteLine("bb2 " + textBox7.Text);
                
                strArg += "-vf \"scale = " + wide + ":-1\" ";
            }


            if (!isIamge && checkBox10.Checked) // 快速剪辑
            {
                if(!radioButton1.Checked && !radioButton2.Checked)
                {
                    strArg += "-c copy ";
                }
                //strArg += "-c:a copy ";
                if (radioButton1.Checked) // 帧同步
                {
                    strArg += "-c copy ";
                    strArg += "-avoid_negative_ts make_zero ";

                }
                else if (radioButton2.Checked)
                {
                    if (checkBox11.Checked)
                    {
                        strArg += "-c:v h264_nvenc ";
                    }
                    else
                    {
                        strArg += "-c:v libx264 ";
                    }
                }
            }
            else if(!isIamge && checkBox9.Checked) // 本地m3u8
            {
                strArg += "-c copy -bsf:a aac_adtstoasc ";
            }
            // 普通剪辑
            else if (!isIamge && processFile.beginTime != null && !"".Equals(processFile.beginTime))
            {
                if (checkBox11.Checked)
                {
                    strArg += "-c:v h264_nvenc ";
                }
                else
                {
                    strArg += "-c:v libx264 ";
                }
                strArg += "-acodec copy -ss " + processFile.beginTime + " -to " + processFile.endTime + " ";
            }
            strArg += processFile.targetFile + " ";

            //Console.WriteLine("bbqq2 " + strArg);


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
