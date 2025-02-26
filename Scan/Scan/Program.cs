using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace Scan
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int start = 1;
            int end = 1028;
            int threads = 200;
            int timeout = 200;

            Console.Write("Enter Target (IP or Domain): ");
            string target = Console.ReadLine();

            // Validate target (basic IP or domain check)
            if (!Uri.IsWellFormedUriString(target, UriKind.Absolute) && !IPAddress.TryParse(target, out _))
            {
                Console.WriteLine("Invalid target format.");
                return;
            }

            Console.Clear();
            Console.Title = $"Scanning {target} from port {start} to {end} using {threads} threads";

            ConsoleColor yellow = ConsoleColor.Yellow;
            Console.WriteLine(@"
            .              +   .                .   . .     .  .  
                   .                    .       .     *  
  .       *                        . . . .  .   .  + .  
            ""Your target here""            .   .  +  . . .  
.                 |             .  .   .    .    . .  
                  |           .     .     . +.    +  .  
                 \|/            .       .   . .  
        . .       V          .    * . . .  .  +   .  
           +      .           .   .      +  
                            .       . +  .+. .  
  .                      .     . + .  . .     .      .  
           .      .    .     . .   . . .        ! /  
      *             .    . .  +    .  .       - O -  
          .     .    .  +   . .  *  .       . / |  
               . + .  .  .  .. +  .  
.      .  .  .  *   .  *  . +..  .            *  
 .      .   . .   .   .   . .  +   .    .            +  
");

            SemaphoreSlim threadLimiter = new SemaphoreSlim(threads);
            Task[] tasks = new Task[end - start + 1];

            int index = 0;
            for (int port = start; port <= end; port++)
            {
                int currentPort = port;

                tasks[index++] = Task.Run(async () =>
                {
                    await threadLimiter.WaitAsync();
                    try
                    {
                        using (TcpClient scan = new TcpClient())
                        {
                            var connectTask = scan.ConnectAsync(target, currentPort);
                            if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                            {
                                lock (Console.Out)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"[+] Port {currentPort} is open");
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[x] Port {currentPort} is closed");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[!] Error scanning port {currentPort}: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                    finally
                    {
                        threadLimiter.Release();
                    }
                });
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("\nScan complete.");
            Console.Write("Press Enter to exit... ");
            Console.ReadKey();
        }
    }
}
