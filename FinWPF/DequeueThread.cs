using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FinWPF
{
    [AddINotifyPropertyChangedInterface]
    public class DequeueThread<T>
    {
        public DequeueThread(ConcurrentQueue<T> queue, Func<List<T>, Task<int>> func)
        {
            _queue = queue;
            _func = func;
        }

        ConcurrentQueue<T> _queue;
        Func<List<T>, Task<int>> _func;

        public bool Disabled { get; set; } = false;
        public int SingleCount { get; set; } = 250;
        public string ThreadName { get; set; }

        public void Run()
        {
            Task.Run(async ()=>{
                while (true)
                {
                    var list = new List<T>();
                    try
                    {
                        try
                        {
                            while (list.Count < SingleCount)
                            {
                                T t;
                                if (!_queue.TryDequeue(out t))
                                {
                                    Thread.Sleep(500);
                                    break;
                                }
                                list.Add(t);
                            }
                        }
                        catch (System.Exception ex1)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info($"Dequeue {ThreadName} exception:{ex1.Message}");
                        }

                        if (list.Any() && !Disabled)
                        {
                            await _func(list);
                        }
                    }
                    catch (Exception ex)
                    {
                        list.ForEach(item => _queue.Enqueue(item));
                        NLog.LogManager.GetCurrentClassLogger().Info($"{ThreadName} exception:{ex.InnerException?.Message}");
                    }
                }
            });
        }
    }
}
