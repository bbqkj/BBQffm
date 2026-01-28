using System;
using System.Timers;

namespace ffm
{
    public class TaskScheduler
    {
        private System.Timers.Timer _timer;

        public TaskScheduler(double intervalMilliseconds)
        {
            _timer = new System.Timers.Timer(intervalMilliseconds);
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = true; // 设置为重复执行
        }

        // 可以添加一个委托事件来通知任务执行
        public event Action OnTaskExecute;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop(); // 停止定时器，避免再次触发
            try
            {
                // 触发事件，执行任务
                OnTaskExecute?.Invoke();
            }
            finally
            {
                _timer.Start(); // 任务完成后重新启动定时器，这样下一次触发将在Interval时间后
            }
            
        }

        // 可以提供一个方法，让外部设置任务
        public void SetTask(Action task)
        {
            OnTaskExecute = null; // 移除之前的事件
            OnTaskExecute += task;
        }

        private void DoTask()
        {
            // 任务逻辑
            Console.WriteLine($"任务执行: {DateTime.Now}");

            // 如果需要更新UI，使用BeginInvoke
            if (System.Windows.Forms.Application.OpenForms.Count > 0)
            {
                var form = System.Windows.Forms.Application.OpenForms[0];
                form.BeginInvoke(new Action(() =>
                {
                    // 更新UI
                }));
            }
        }
    }
}
