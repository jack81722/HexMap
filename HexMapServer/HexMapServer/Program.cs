using System;
using System.Threading;
using System.Threading.Tasks;

namespace HexMapServer
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //CancellationTokenSource cts = new CancellationTokenSource();
            //Server server = new Server();
            //Console.WriteLine($"Server Launching...");
            //Task<IServerResult> mainTask = Task.Run<IServerResult>(() => server.Start());
            //var endTask = mainTask.ContinueWith(
            //    (task) =>
            //    {
            //        if (task.IsCompletedSuccessfully)
            //            Console.WriteLine(task.Result);
            //        else if (task.IsCanceled)
            //            Console.WriteLine("Task was canceled.");
            //        else if (task.IsFaulted)
            //            Console.WriteLine(task.Exception);
            //    });

            //while (!cts.Token.IsCancellationRequested)
            //{
            //    var cmd = Console.ReadLine();
            //    Console.WriteLine($"Cmd:{cmd}");
            //    if (cmd.ToLower().Equals("exit"))
            //    {
            //        server.Close();
            //        cts.Cancel();
            //    }
                
            //}
            //endTask.Wait();
            Console.ReadLine();
        }
    }
}
