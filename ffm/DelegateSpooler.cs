using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ffm
{
    internal class DelegateSpooler
    {
        private Thread worker;

        private List<ThreadStart> delegates = new List<ThreadStart>();

        private Dictionary<int, KeyValuePair<ThreadStart, IAsyncResult>> execed = new Dictionary<int, KeyValuePair<ThreadStart, IAsyncResult>>();

        internal DelegateSpooler(string name)
        {
            worker = new Thread(bgWorker);
            worker.Name = typeof(DelegateSpooler).Name + "<" + name + ">";
            worker.IsBackground = true;
            worker.Start();
        }

        public void Set(int pipe, ThreadStart del)
        {
            lock (delegates)
            {
                if (0 <= pipe && pipe < delegates.Count)
                {
                    delegates[pipe] = del;
                }
            }
        }

        internal void ClearQueue()
        {
            lock (delegates)
            {
                delegates.Clear();
            }
        }

        internal void InitQueue(int pipes)
        {
            lock (delegates)
            {
                delegates.Clear();
                delegates.AddRange(new ThreadStart[pipes]);
            }
        }

        internal bool IsPipeExecuting(int pipe)
        {
            return execed.ContainsKey(pipe);
        }

        private void bgWorker()
        {
            while (true)
            {
                try
                {
                    foreach (int item in new List<int>(execed.Keys))
                    {
                        if (execed[item].Value.IsCompleted)
                        {
                            try
                            {
                                execed[item].Key.EndInvoke(execed[item].Value);
                            }
                            catch
                            {
                            }

                            execed.Remove(item);
                        }
                    }

                    lock (delegates)
                    {
                        for (int i = 0; i < delegates.Count; i++)
                        {
                            if (delegates[i] != null && !execed.ContainsKey(i))
                            {
                                ThreadStart threadStart = delegates[i];
                                execed[i] = new KeyValuePair<ThreadStart, IAsyncResult>(threadStart, threadStart.BeginInvoke(null, null));
                                delegates[i] = null;
                            }
                        }
                    }

                    Thread.Sleep(10);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch
                {
                }
            }
        }
    }
}
