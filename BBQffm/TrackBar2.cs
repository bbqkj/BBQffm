using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ffm
{

    /// <summary>
    /// 类    名:LhxTrackBar2
    /// 描    述:双头滑块
    /// 修改时间:2022/9/30 16:36:48
    /// </summary>
    public partial class TrackBar2 : Control
    {



        #region 成员变量

        #region private


        /// <summary>
        /// 最小的控件尺寸
        /// </summary>
        private const int m_MinSize = 100;


        /// <summary>
        /// 刻度线大小
        /// </summary>
        private const int m_tickSize = 6;

        /// <summary>
        /// 滑轨大小
        /// </summary>
        private const int m_TrackSize = 5;



        private enum emBtnStatus
        {
            None,

            Btn1MouseIn,

            Btn2MouseIn,

            Btn1MouseDown,

            Btn2MouseDown
        }

        /// <summary>
        /// 按键的状态
        /// </summary>
        private emBtnStatus m_btnStatus = emBtnStatus.None;

        /// <summary>
        /// 最值区间与UI显示区间比例
        /// </summary>
        private double m_dbRate = 0;

        /// <summary>
        /// 上一次鼠标的位置
        /// </summary>
        private Point m_lastMouseLocation;



        private double m_dbMinimum = 0;

        private double m_dbMaximum = 100;

        private double m_dbValue1 = 30;

        private double m_dbValue2 = 70;
        #endregion


        #region protected



        #endregion


        #region public


        public override Size MinimumSize
        {
            get
            {
                Size rlt = base.MinimumSize;
                if (this.Orientation == Orientation.Horizontal)
                {
                    rlt = new Size(m_MinSize, 0);
                }
                else
                {
                    rlt = new Size(0, m_MinSize);
                }

                return rlt;
            }
            set => base.MinimumSize = value;
        }


        /// <summary>
        /// 最小值
        /// </summary>
        public double Minimum
        {
            get
            {
                return m_dbMinimum;
            }
            set
            {
                if (value <= Maximum)
                {
                    m_dbMinimum = value;
                    this.Invalidate();
                }

            }
        }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Maximum
        {
            get
            {
                return m_dbMaximum;
            }
            set
            {
                if (value > Minimum)
                {
                    m_dbMaximum = value;
                    this.Invalidate();
                }
            }
        }


        /// <summary>
        /// 数值1
        /// </summary>
        public double Value1
        {
            get
            {
                return m_dbValue1;
            }
            set
            {
                if (value < Value2 && value >= Minimum)
                {
                    m_dbValue1 = value;
                    Value1Changed?.Invoke(this, EventArgs.Empty);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// 数值2
        /// </summary>
        public double Value2
        {
            get
            {
                return m_dbValue2;
            }
            set
            {
                if (value > Value1 && value <= Maximum)
                {
                    m_dbValue2 = value;
                    Value2Changed?.Invoke(this, EventArgs.Empty);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }
            }
        }


        private Orientation m_Orientation = Orientation.Horizontal;

        /// <summary>
        /// 绘制的布局方式
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return m_Orientation;
            }
            set
            {
                if (m_Orientation != value)
                {
                    m_Orientation = value;
                    this.Invalidate();
                }
            }
        }


        /// <summary>
        /// 刻度标签是否可见
        /// </summary>
        public bool TickLabelVisible { get; set; } = true;

        /// <summary>
        /// 最小值最大值标签显示的小数点位数
        /// </summary>
        public uint LabelPlaces { get; set; } = 1;

        /// <summary>
        /// 按键1的位置
        /// </summary>
        private Rectangle m_rectBtn1;

        /// <summary>
        /// 按键2的位置
        /// </summary>
        private Rectangle m_rectBtn2;



        public enum emTrackBarSelectedMode
        {
            /// <summary>
            /// 选中两个滑块内部的值
            /// </summary>
            Inner,

            /// <summary>
            /// 选中两个滑块外部的值
            /// </summary>
            Outer
        }

        /// <summary>
        /// 刻度线个数
        /// </summary>
        public int TickCount { get; set; } = 5;

        /// <summary>
        /// 刻度线颜色
        /// </summary>
        public Color TickColor { get; set; } = Color.Black;

        /// <summary>
        /// 滑块选择模式
        /// </summary>
        public emTrackBarSelectedMode TrackSelctedMode
        {
            get; set;
        } = emTrackBarSelectedMode.Inner;

        /// <summary>
        /// 选中滑轨部分的颜色
        /// </summary>
        public Color SelectTrackColor { get; set; } = Color.Green;

        /// <summary>
        /// 滑轨的颜色
        /// </summary>
        public Color TrackColor { get; set; } = Color.Black;

        /// <summary>
        /// 滑块1按键颜色
        /// </summary>
        public Color TrackButtonColor1 { get; set; } = Color.DarkGray;

        /// <summary>
        /// 滑块2按键颜色
        /// </summary>
        public Color TrackButtonColor2 { get; set; } = Color.DarkGray;

        /// <summary>
        /// 滑块键入或者按下的颜色
        /// </summary>
        public Color TrackButtonClickColor { get; set; } = Color.Blue;

        [Browsable(true)]
        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override bool AutoSize { get => base.AutoSize; set => base.AutoSize = value; }

        /// <summary>
        /// 滑块的大小
        /// </summary>
        public Size TrackButtonSize { get; set; } = new Size(28, 12);
        #endregion

        #region event

        public event EventHandler Value1Changed;

        public event EventHandler Value2Changed;

        public event EventHandler ValueChanged;


        #endregion

        #endregion


        #region 构造函数
        /// <summary>
        /// 函 数 名:构造函数
        /// 函数描述:默认构造函数
        /// 修改时间:2022/9/30 16:36:48
        /// </summary>
        public TrackBar2()
        {
            this.DoubleBuffered = true;
            this.SizeChanged += OnLhxTrackBar2SizeChanged;
        }



        #endregion

        #region 父类函数重载、接口实现


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (m_rectBtn1.Contains(e.X, e.Y))
            {
                m_btnStatus = emBtnStatus.Btn1MouseDown;
                m_lastMouseLocation = e.Location;
                this.Invalidate();
            }
            else if (m_rectBtn2.Contains(e.X, e.Y))
            {
                m_btnStatus = emBtnStatus.Btn2MouseDown;
                m_lastMouseLocation = e.Location;
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            m_btnStatus = emBtnStatus.None;
        }


        /// <summary>
        /// 鼠标移动的事件,负责滑块移动以及滑块键入改色
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (m_btnStatus == emBtnStatus.Btn1MouseDown)
            {
                if (m_dbRate != 0)
                {
                    this.Value1 += GetMouseChangedValue(e.Location, m_lastMouseLocation);
                }
                m_lastMouseLocation = e.Location;
            }
            else if (m_btnStatus == emBtnStatus.Btn2MouseDown)
            {
                if (m_dbRate != 0)
                {
                    this.Value2 += GetMouseChangedValue(e.Location, m_lastMouseLocation);
                }
                m_lastMouseLocation = e.Location;
            }
            else if (m_rectBtn1.Contains(e.X, e.Y))
            {
                if (m_btnStatus != emBtnStatus.Btn1MouseIn)
                {
                    m_btnStatus = emBtnStatus.Btn1MouseIn;
                    this.Refresh();
                }
            }
            else if (m_rectBtn2.Contains(e.X, e.Y))
            {
                if (m_btnStatus != emBtnStatus.Btn2MouseIn)
                {
                    m_btnStatus = emBtnStatus.Btn2MouseIn;
                    this.Refresh();
                }
            }
            else
            {
                if (m_btnStatus != emBtnStatus.None)
                {
                    m_btnStatus = emBtnStatus.None;
                    this.Refresh();
                }
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            this.SizeChanged -= OnLhxTrackBar2SizeChanged;
            base.OnPaint(e);
            e.Graphics.Clear(this.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;  //使绘图质量最高，即消除锯齿
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            if (this.Orientation == Orientation.Horizontal)
            {
                OnPaintHorizontal(e);
            }
            else
            {
                OnPaintVertical(e);
            }
            this.SizeChanged += OnLhxTrackBar2SizeChanged;
        }



        #endregion

        #region 函数

        #region private


        /// <summary>
        /// 校验数值是否超出最值
        /// </summary>
        private void CheckValue()
        {
            if (this.m_dbMaximum > Maximum)
            {
                m_dbMaximum = Maximum;
            }

            if (m_dbMinimum < Minimum)
            {
                m_dbMinimum = Minimum;
            }
        }

        /// <summary>
        /// 获取鼠标的变化值
        /// </summary>
        /// <param name="nowLocation"></param>
        /// <param name="oldLocation"></param>
        /// <returns></returns>
        private double GetMouseChangedValue(Point nowLocation, Point oldLocation)
        {
            double rlt = 0;
            if (this.Orientation == Orientation.Horizontal)
            {
                rlt = nowLocation.X - oldLocation.X;
            }
            else
            {
                rlt = nowLocation.Y - oldLocation.Y;
            }
            return rlt / m_dbRate;
        }

        /// <summary>
        /// 获取最大值的绘制内容
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        private void GetMaximumPaintInfo(Graphics g, out string text, out SizeF size)
        {
            GetValuePaintInfo(g, Maximum, out text, out size);
        }

        /// <summary>
        /// 获取最小值的绘制内容
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        private void GetMinimumPaintInfo(Graphics g, out string text, out SizeF size)
        {
            GetValuePaintInfo(g, Minimum, out text, out size);
        }


        /// <summary>
        /// 获取数值类的
        /// </summary>
        /// <param name="g"></param>
        /// <param name="value"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        private void GetValuePaintInfo(Graphics g, double value, out string text, out SizeF size)
        {
            text = value.ToString($"f{LabelPlaces}");
            size = g.MeasureString(text, this.Font);
        }




        #endregion


        #region protected

        private void OnLhxTrackBar2SizeChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        /// <summary>
        /// 绘制水平布局时的UI
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintHorizontal(PaintEventArgs e)
        {

            double left = 0; //滑轨左侧的坐标
            double right = this.Width; //滑轨右侧的坐标

            int tickBottom = this.Height; //刻度线底部
            int tickTop = tickBottom - m_tickSize; //刻度线顶部

            int control_height = 0;
            int btnSize = TrackButtonSize.Width;
            if (TickLabelVisible)
            {
                #region 画刻度线
                string maxText, minText;
                SizeF maxSize, minSize;
                GetMaximumPaintInfo(e.Graphics, out maxText, out maxSize);
                GetMinimumPaintInfo(e.Graphics, out minText, out minSize);

                tickTop -= (int)minSize.Height;
                tickBottom -= (int)minSize.Height + 1;

                left += Math.Max(btnSize / 2, minSize.Width / 2);
                right -= Math.Max(btnSize / 2, maxSize.Width / 2);

                int tick_y = tickBottom;

                Pen pen_tick = new Pen(this.TickColor, 1f);
                //画水平的刻度线
                e.Graphics.DrawLine(pen_tick,
                    new Point((int)left, tick_y),
                      new Point((int)right, tick_y));

                double tickStep = (right - left) / TickCount;
                double valueStep = (Maximum - Minimum) / TickCount;
                for (int i = 0; i <= TickCount; i++)
                {
                    int x = (int)(left + i * tickStep);
                    e.Graphics.DrawLine(pen_tick,
                        new Point(x, tick_y),
                        new Point(x, tick_y - m_tickSize));

                    double value = Minimum + valueStep * i;
                    string text;
                    SizeF size;
                    GetValuePaintInfo(e.Graphics, value, out text, out size);
                    e.Graphics.DrawString(text, this.Font, new SolidBrush(ForeColor),
                   new PointF(x - size.Width / 2, (this.Height - minSize.Height)));

                    control_height = Math.Max(control_height, Convert.ToInt32(size.Height) + 1);
                }
                control_height += this.TrackButtonSize.Height;
                #endregion
            }
            else
            {
                double spacing = btnSize / 2;
                left += spacing;
                right -= spacing;
            }

            if (this.AutoSize)
            {
                this.Height = control_height;
            }

            #region 画滑块

            int trackBtnTop = 2; //滑块顶部坐标
            int trackBtnHeight = tickTop - trackBtnTop; //滑块高度
            int trackBtnBottom = tickTop; //滑块底部坐标

            Pen pen_track = new Pen(new SolidBrush(TrackColor), 2);

            int trackTop = (tickTop - m_TrackSize) / 2; //滑轨顶部坐标
            int trackHeight = m_TrackSize; //滑轨高度

            //画空滑轨
            e.Graphics.DrawRectangle(pen_track,
                new Rectangle((int)left, trackTop, (int)(right - left), trackHeight));


            //画选中的滑块部分
            double rate = (right - left) / (Maximum - Minimum);
            m_dbRate = rate;

            int value1 = (int)((Value1 - Minimum) * rate);
            int value2 = (int)((Value2 - Minimum) * rate);
            int value1_x = (int)(left + value1);
            int value2_x = (int)(left + value2);
            //画选中部分
            if (TrackSelctedMode == emTrackBarSelectedMode.Inner)
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                    new Rectangle((int)(value1_x), trackTop, (int)(value2 - value1), trackHeight));
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                    new Rectangle((int)(left), trackTop, (int)(value1), m_TrackSize));
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                   new Rectangle((int)(value2_x), trackTop, (int)(right - value2 - left), trackHeight));
            }


            //画左侧滑块
            e.Graphics.FillPolygon(new SolidBrush(
                m_btnStatus == emBtnStatus.Btn1MouseDown || m_btnStatus == emBtnStatus.Btn1MouseIn ? TrackButtonClickColor : TrackButtonColor1),
                new Point[] {
                new Point(value1_x - btnSize/2,trackBtnTop),
                 new Point(value1_x + btnSize/2,trackBtnTop),
                  new Point(value1_x + btnSize/2,trackBtnBottom*2/3),
                   new Point(value1_x,trackBtnBottom),
                   new Point(value1_x- btnSize/2,trackBtnBottom*2/3)
            });

            e.Graphics.FillPolygon(new SolidBrush(
                  m_btnStatus == emBtnStatus.Btn2MouseDown || m_btnStatus == emBtnStatus.Btn2MouseIn ? TrackButtonClickColor : TrackButtonColor2),
                new Point[] {
                new Point(value2_x - btnSize/2,trackBtnTop),
                 new Point(value2_x + btnSize/2,trackBtnTop),
                  new Point(value2_x + btnSize/2,trackBtnBottom*2/3),
                   new Point(value2_x,trackBtnBottom),
                   new Point(value2_x- btnSize/2,trackBtnBottom*2/3)
            });
            m_rectBtn1 = new Rectangle(value1_x - btnSize / 2, trackBtnTop, btnSize, trackBtnHeight);
            m_rectBtn2 = new Rectangle(value2_x - btnSize / 2, trackBtnTop, btnSize, trackBtnHeight);
            #endregion
        }


        /// <summary>
        /// 绘制垂直布局时的UI
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintVertical(PaintEventArgs e)
        {

            double top = 0; //滑轨顶部的坐标
            double bottom = this.Height; //滑轨底部的坐标

            int tickRight = this.Width; //刻度线右侧坐标
            int tickLeft = tickRight - m_tickSize; //刻度线左侧坐标

            int btnSize = TrackButtonSize.Height;
            int control_width = 0;
            if (TickLabelVisible)
            {
                #region 画刻度线
                string maxText, minText;
                SizeF maxSize, minSize;
                GetMaximumPaintInfo(e.Graphics, out maxText, out maxSize);
                GetMinimumPaintInfo(e.Graphics, out minText, out minSize);

                tickLeft -= (int)maxSize.Width;
                tickRight -= (int)maxSize.Width + 1;

                top += Math.Max(btnSize / 2, minSize.Height / 2);
                bottom -= Math.Max(btnSize / 2, maxSize.Height / 2);

                int tick_x = tickRight;

                Pen pen_tick = new Pen(this.TickColor, 1f);
                //画水平的刻度线
                e.Graphics.DrawLine(pen_tick,
                    new Point(tick_x, (int)top),
                      new Point(tick_x, (int)bottom));

                double tickStep = (bottom - top) / TickCount;
                double valueStep = (Maximum - Minimum) / TickCount;
                for (int i = 0; i <= TickCount; i++)
                {
                    int y = (int)(top + i * tickStep);
                    e.Graphics.DrawLine(pen_tick,
                        new Point(tick_x, y),
                        new Point(tick_x - m_tickSize, y));

                    double value = Minimum + valueStep * i;
                    string text;
                    SizeF size;
                    GetValuePaintInfo(e.Graphics, value, out text, out size);
                    e.Graphics.DrawString(text, this.Font, new SolidBrush(ForeColor),
                        new PointF(tickRight, (y - minSize.Height / 2)));

                    control_width = Math.Max(control_width, Convert.ToInt32(size.Width) + 1);
                }
                control_width += this.TrackButtonSize.Width + m_tickSize;
                #endregion
            }
            else
            {
                double spacing = btnSize / 2;
                top += spacing;
                bottom -= spacing;
            }
            if (AutoSize)
            {
                this.Width = control_width;
            }

            #region 画滑块

            int trackBtnLeft = 2; //滑块左侧坐标
            int trackBtnWidth = tickLeft - trackBtnLeft; //滑块宽度
            int trackBtnRight = tickLeft; //滑块右侧坐标

            Pen pen_track = new Pen(new SolidBrush(TrackColor), 2);

            int trackLeft = (tickLeft - m_TrackSize) / 2; //滑轨左侧坐标
            int trackWidth = m_TrackSize; //滑轨高度

            //画空滑轨
            e.Graphics.DrawRectangle(pen_track,
                new Rectangle(trackLeft, (int)top, trackWidth, (int)(bottom - top)));


            //画选中的滑块部分
            double rate = (bottom - top) / (Maximum - Minimum);
            m_dbRate = rate;

            int value1 = (int)((Value1 - Minimum) * rate);
            int value2 = (int)((Value2 - Minimum) * rate);
            int value1_y = (int)(top + value1);
            int value2_y = (int)(top + value2);
            //画选中部分
            if (TrackSelctedMode == emTrackBarSelectedMode.Inner)
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                    new Rectangle(trackLeft, (int)value1_y, trackWidth, (int)(value2 - value1)));
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                    new Rectangle((int)(trackLeft), (int)top, trackWidth, (int)value1));
                e.Graphics.FillRectangle(new SolidBrush(SelectTrackColor),
                   new Rectangle((int)(trackLeft), (int)value2_y, trackWidth, (int)(bottom - value2 - top)));
            }


            //画左侧滑块
            e.Graphics.FillPolygon(new SolidBrush(
                m_btnStatus == emBtnStatus.Btn1MouseDown || m_btnStatus == emBtnStatus.Btn1MouseIn ? TrackButtonClickColor : TrackButtonColor1),
                new Point[] {
                new Point(trackBtnLeft,value1_y - btnSize/2),
                 new Point(trackBtnLeft,value1_y + btnSize/2),
                  new Point(trackBtnRight *2/3,value1_y + btnSize/2),
                   new Point(trackBtnRight,value1_y),
                   new Point(trackBtnRight *2/3,value1_y - btnSize/2)
            });

            e.Graphics.FillPolygon(new SolidBrush(
                  m_btnStatus == emBtnStatus.Btn2MouseDown || m_btnStatus == emBtnStatus.Btn2MouseIn ? TrackButtonClickColor : TrackButtonColor2),
                new Point[] {
                  new Point(trackBtnLeft,value2_y - btnSize/2),
                 new Point(trackBtnLeft,value2_y + btnSize/2),
                  new Point(trackBtnRight *2/3,value2_y + btnSize/2),
                   new Point(trackBtnRight,value2_y),
                   new Point(trackBtnRight *2/3,value2_y - btnSize/2)
            });
            m_rectBtn1 = new Rectangle(trackBtnLeft, value1_y - btnSize / 2, trackBtnWidth, btnSize);
            m_rectBtn2 = new Rectangle(trackBtnLeft, value2_y - btnSize / 2, trackBtnWidth, btnSize);
            #endregion
        }

        #endregion


        #region public


        /// <summary>
        /// 设置最值
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        public void SetMinMax(double minimum, double maximum)
        {
            if (minimum < maximum)
            {
                this.m_dbMinimum = minimum;
                this.m_dbMaximum = maximum;
                CheckValue();
                this.Invalidate();
            }
        }


        /// <summary>
        /// 设置数值
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        public void SetValue(double value1, double value2)
        {
            if (value1 < value2)
            {
                this.m_dbValue1 = value1;
                this.m_dbValue2 = value2;
                CheckValue();
                this.Invalidate();
            }
        }


        /// <summary>
        /// 设置数值和最值
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        public void SetValue(double minimum, double maximum, double value1, double value2)
        {
            if (minimum < maximum)
            {
                this.m_dbMinimum = minimum;
                this.m_dbMaximum = maximum;
            }
            if (value1 < value2)
            {
                this.m_dbValue1 = value1;
                this.m_dbValue2 = value2;
            }
            this.CheckValue();
            this.Invalidate();
        }
        #endregion

        #endregion

    }
}
