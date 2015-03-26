using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learn.Task2
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                    Task.Factory.StartNew(o =>
                    {
                        Console.WriteLine(o);
                        Task.Delay(500).Wait();
                        throw new Exception("MyException");
                    }, i, TaskCreationOptions.LongRunning);
            });
            Console.ReadKey();
        }
    }
}
