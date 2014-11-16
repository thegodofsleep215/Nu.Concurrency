using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nu.Concurrency;

namespace Sandbox
{
    class Program
    {
        static Random rand = new Random();
        static void Main(string[] args)
        {
            var channel1 = GenerateNumbers("channel1");
            var channel2 = GenerateNumbers("channel2");
            
            var bob = ConsumeChannel("bob", Channel<string>.Merge(channel1, channel2));

            bob.Wait();

        }

        private static Channel<string> GenerateNumbers(string name)
        {
            var channel = new Channel<string>();

            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i <10 ; i++)
                {
                    channel.Push(string.Format("{0}: {1}", name, i));
                }
                channel.Close();
            });
            return channel;
        }

        private static Task ConsumeChannel<T>(string text, Channel<T> channel)
        {
            return Task.Factory.StartNew(() =>
            {
                T item;
                while(channel.Retreive(out item))
                {
                    Console.WriteLine("{0}: {1}", text, item);
                    Thread.Sleep(rand.Next(0, 1001));
                }
            });
        }
    }
} 