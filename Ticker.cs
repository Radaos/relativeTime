using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Program
{
    // Counter A variables
    private static long _counterA;
    private static readonly object _lockA = new object();
    private static CancellationTokenSource _cancellationTokenSourceA;

    // Counter B variables  
    private static long _counterB;
    private static readonly object _lockB = new object();
    private static CancellationTokenSource _cancellationTokenSourceB;

    private const long MaxSecondsInDay = 86400; // 24 * 60 * 60

    public static void Main(string[] args)
    {
        InitializeCounters();
        StartCounterThreads();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Run the application - this blocks until the form is closed
        Application.Run(new MainForm());

        // Clean up the threads when the application exits
        StopCounterThreads();
    }

    /// <summary>
    /// Initializes both counters to the number of seconds elapsed since midnight.
    /// </summary>
    private static void InitializeCounters()
    {
        long initialSeconds = (long)DateTime.Now.TimeOfDay.TotalSeconds;

        lock (_lockA)
        {
            _counterA = initialSeconds;
        }

        lock (_lockB)
        {
            _counterB = initialSeconds;
        }

    }

    private static async Task RunCounter(CancellationToken cancellationToken, Func<long> getCounter, Action<long> setCounter, object lockObj, string counterName)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellationToken);
                lock (lockObj)
                {
                    long counter = getCounter();
                    counter++;
                    if (counter >= MaxSecondsInDay)
                    {
                        counter = 0;
                    }
                    setCounter(counter);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"{counterName} thread cancelled");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{counterName} thread error: {ex}");
                break;
            }
        }
    }

    /// <summary>
    /// Starts both counter threads with precise timing alignment.
    /// </summary>
    private static void StartCounterThreads()
    {
        _cancellationTokenSourceA = new CancellationTokenSource();
        _cancellationTokenSourceB = new CancellationTokenSource();

        // Calculate initial delay to align with the next second boundary
        var now = DateTime.Now;
        int initialDelay = 1000 - now.Millisecond;

        // Start Counter A thread
        Task.Run(async () =>
        {
            // Wait for alignment
            await Task.Delay(initialDelay, _cancellationTokenSourceA.Token);

            await RunCounter(
                _cancellationTokenSourceA.Token,
                () => _counterA,
                val => _counterA = val,
                _lockA,
                "Counter A"
            );
        });

        // Start Counter B thread  
        Task.Run(async () =>
        {
            // Wait for alignment (same delay to start synchronized)
            await Task.Delay(initialDelay, _cancellationTokenSourceB.Token);

            await RunCounter(
                _cancellationTokenSourceB.Token,
                () => _counterB,
                val => _counterB = val,
                _lockB,
                "Counter B"
            );
        });

    }

    /// <summary>
    /// Stops both counter threads gracefully.
    /// </summary>
    private static void StopCounterThreads()
    {
        Console.WriteLine("Stopping counter threads...");

        _cancellationTokenSourceA?.Cancel();
        _cancellationTokenSourceB?.Cancel();

        _cancellationTokenSourceA?.Dispose();
        _cancellationTokenSourceB?.Dispose();

        Console.WriteLine("Counter threads stopped");
    }

    /// <summary>
    /// Gets the current value of Counter A (thread-safe).
    /// </summary>
    public static long GetCounterA()
    {
        lock (_lockA)
        {
            return _counterA;
        }
    }

    /// <summary>
    /// Gets the current value of Counter B (thread-safe).
    /// </summary>
    public static long GetCounterB()
    {
        lock (_lockB)
        {
            return _counterB;
        }
    }
}
