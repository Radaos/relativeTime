using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

public partial class MainForm : Form
{
    // Counter A (Earth time) - Timer instance used for periodic updates
    private static System.Timers.Timer _timerA;
    private static long _counterA;
    private static readonly object _lockA = new object();

    // Counter B (Relativistic time) - Independent timer and counter
    private static System.Timers.Timer _timerB;
    private static double _counterB;
    private static readonly object _lockB = new object();
    private static double _velocityFraction = 0.0; // Fraction of speed of light (0 to 0.999)
    private static double _timeDilationFactor = 1.0; // Lorentz factor

    private const long MaxSecondsInDay = 86400; // 24 * 60 * 60

    private Label titleLabel;
    private Label instructionLabel;
    private Label counterALabel;
    private Label counterBLabel;
    private Label differenceLabel;
    private Label velocityLabel;
    private Label timeLabel;
    private Button exitButton;
    private Button increaseSpeedButton;
    private Button decreaseSpeedButton;

    public MainForm()
    {
        InitializeComponent();
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(value);
        if (value && !DesignMode)
        {
            // Initialize after the form is visible and handle is created
            InitializeCounters();
            StartTimers();
        }
    }

    private void InitializeComponent()
    {
        Text = "Relativistic Time Dilation Counter";
        Size = new Size(600, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // Title label
        titleLabel = new Label
        {
            Text = "--- Relativistic Time Dilation Counter ---",
            Location = new Point(20, 20),
            Size = new Size(550, 20),
            Font = new Font("Arial", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(titleLabel);

        // Instruction label
        instructionLabel = new Label
        {
            Text = "Counter A: Earth Time | Counter B: Observer Time at relativistic speeds",
            Location = new Point(20, 45),
            Size = new Size(550, 15),
            Font = new Font("Arial", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(instructionLabel);

        // Separator line (simulated with label)
        var separatorLabel = new Label
        {
            Text = "--------------------------------------------------------------",
            Location = new Point(20, 65),
            Size = new Size(550, 15),
            Font = new Font("Consolas", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(separatorLabel);

        // Counter A display label (Earth time)
        counterALabel = new Label
        {
            Text = "Counter A (Earth): 00000 | Current Time: 00:00:00",
            Location = new Point(20, 115),
            Size = new Size(550, 30),
            Font = new Font("Consolas", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Black,
            ForeColor = Color.LimeGreen
        };
        Controls.Add(counterALabel);

        // Counter B display label (Relativistic time)
        counterBLabel = new Label
        {
            Text = "Counter B (Observer): 00000.00 | Time Dilation Factor: 1.000",
            Location = new Point(20, 155),
            Size = new Size(550, 30),
            Font = new Font("Consolas", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.DarkBlue,
            ForeColor = Color.Cyan
        };
        Controls.Add(counterBLabel);

        differenceLabel = new Label
        {
            Text = "Time Difference (A - B): 0.00 seconds",
            Location = new Point(20, 195),
            Size = new Size(550, 30),
            Font = new Font("Consolas", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.DarkMagenta,
            ForeColor = Color.Yellow
        };
        Controls.Add(differenceLabel);

        // Velocity control label
        velocityLabel = new Label
        {
            Text = "Observer Velocity: 0.000c",
            Location = new Point(20, 235),
            Size = new Size(250, 25),
            Font = new Font("Arial", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DarkRed
        };
        Controls.Add(velocityLabel);

        // Decrease speed button
        decreaseSpeedButton = new Button
        {
            Text = "- 0.05c",
            Location = new Point(280, 235),
            Size = new Size(80, 25),
            Font = new Font("Arial", 9)
        };
        decreaseSpeedButton.Click += DecreaseSpeed_Click;
        Controls.Add(decreaseSpeedButton);

        // Increase speed button
        increaseSpeedButton = new Button
        {
            Text = "+ 0.05c",
            Location = new Point(370, 235),
            Size = new Size(80, 25),
            Font = new Font("Arial", 9)
        };
        increaseSpeedButton.Click += IncreaseSpeed_Click;
        Controls.Add(increaseSpeedButton);

        // Time status label (for midnight resets)
        timeLabel = new Label
        {
            Text = "",
            Location = new Point(20, 275),
            Size = new Size(550, 20),
            Font = new Font("Consolas", 9),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Orange
        };
        Controls.Add(timeLabel);

        // Physics explanation label
        var physicsLabel = new Label
        {
            Text = "Time dilation: t' = t / √(1 - v²/c²) | Observer experiences slower time at high speeds",
            Location = new Point(20, 305),
            Size = new Size(550, 30),
            Font = new Font("Arial", 8, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray
        };
        Controls.Add(physicsLabel);

        // Exit button
        exitButton = new Button
        {
            Text = "Exit",
            Location = new Point(250, 350),
            Size = new Size(100, 30),
            Font = new Font("Arial", 10)
        };
        exitButton.Click += ExitButton_Click;
        Controls.Add(exitButton);

        // Handle form closing
        FormClosing += MainForm_FormClosing;
    }

    /// <summary>
    /// Initializes both counters to the number of seconds elapsed since midnight.
    /// </summary>
    private void InitializeCounters()
    {
        long initialSeconds = (long)DateTime.Now.TimeOfDay.TotalSeconds;
        _counterA = initialSeconds;
        _counterB = initialSeconds; // Ensure Counter B starts with the exact same value

        UpdateTimeDilationFactor();
    }

    /// <summary>
    /// Configures and starts both timers.
    /// </summary>
    private async void StartTimers()
    {
        // Calculate the initial delay to align to the next full second
        var now = DateTime.Now;
        int initialDelay = 1000 - now.Millisecond;

        // Set up timer A (Earth time) - fires every 1000ms (1 second)
        _timerA = new System.Timers.Timer(1000)
        {
            AutoReset = true,
            Enabled = false
        };
        _timerA.Elapsed += OnTimedEventA;

        // Set up timer B (Relativistic time) - fires every 1000ms (1 second) - CHANGED from 100ms
        _timerB = new System.Timers.Timer(1000)
        {
            AutoReset = true,
            Enabled = false
        };
        _timerB.Elapsed += OnTimedEventB;

        // Wait for the calculated delay asynchronously
        await Task.Delay(initialDelay);

        _timerA.Enabled = true;
        _timerB.Enabled = true;

        // Update displays immediately
        lock (_lockA)
        {
            UpdateDisplayA();
        }
        lock (_lockB)
        {
            UpdateDisplayB();
        }
        UpdateDifferenceDisplay();
    }

    /// <summary>
    /// Timer A event - runs every second (Earth time).
    /// </summary>
    private void OnTimedEventA(object source, ElapsedEventArgs e)
    {
        lock (_lockA)
        {
            _counterA++;

            // Handle Midnight Reset for Counter A
            if (_counterA >= MaxSecondsInDay)
            {
                _counterA = 0;
                DisplayMidnightReset("A");
            }

            UpdateDisplayA();
        }
        UpdateDifferenceDisplay(); // Update difference when Counter A changes
    }

    /// <summary>
    /// Timer B event - runs every 1000ms (Relativistic time).
    /// </summary>
    private void OnTimedEventB(object source, ElapsedEventArgs e)
    {
        lock (_lockB)
        {
            // When velocity is 0 (time dilation factor = 1.0), Counter B should run at the same rate as Counter A
            // Increment by 1.0 seconds divided by the time dilation factor - CHANGED from 0.1
            double increment = 1.0 / _timeDilationFactor;
            _counterB += increment;

            // Handle Midnight Reset for Counter B
            if (_counterB >= MaxSecondsInDay)
            {
                _counterB = 0;
                DisplayMidnightReset("B");
            }

            UpdateDisplayB();
        }
    }

    /// <summary>
    /// Updates the display for Counter A (Earth time).
    /// </summary>
    private void UpdateDisplayA()
    {
        string formattedCounter = _counterA.ToString("D5");

        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                counterALabel.Text = $"Counter A (Earth): {formattedCounter} | Current Time: {DateTime.Now:HH:mm:ss}";
            }));
        }
        else
        {
            counterALabel.Text = $"Counter A (Earth): {formattedCounter} | Current Time: {DateTime.Now:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Updates the display for Counter B (Relativistic time).
    /// </summary>
    private void UpdateDisplayB()
    {
        string formattedCounter = _counterB.ToString("00000.00");

        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                counterBLabel.Text = $"Counter B (Observer): {formattedCounter} | Time Dilation Factor: {_timeDilationFactor:F3}";
            }));
        }
        else
        {
            counterBLabel.Text = $"Counter B (Observer): {formattedCounter} | Time Dilation Factor: {_timeDilationFactor:F3}";
        }
    }

    /// <summary>
    /// Updates the difference display showing the time difference between Counter A and Counter B.
    /// </summary>
    private void UpdateDifferenceDisplay()
    {
        double difference;
        lock (_lockA)
        {
            lock (_lockB)
            {
                difference = _counterA - _counterB;
            }
        }

        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                differenceLabel.Text = $"Time Difference (A - B): {difference:F2} seconds";
            }));
        }
        else
        {
            differenceLabel.Text = $"Time Difference (A - B): {difference:F2} seconds";
        }
    }

    /// <summary>
    /// Calculates the time dilation factor based on velocity.
    /// </summary>
    private void UpdateTimeDilationFactor()
    {
        // Lorentz factor: γ = 1 / √(1 - v²/c²)
        double vSquaredOverCSquared = _velocityFraction * _velocityFraction;
        _timeDilationFactor = 1.0 / Math.Sqrt(1.0 - vSquaredOverCSquared);

        // Update velocity display
        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                velocityLabel.Text = $"Observer Velocity: {_velocityFraction:F3}c";
            }));
        }
        else
        {
            velocityLabel.Text = $"Observer Velocity: {_velocityFraction:F3}c";
        }
    }

    private void IncreaseSpeed_Click(object sender, EventArgs e)
    {
        lock (_lockB)
        {
            _velocityFraction = Math.Min(_velocityFraction + 0.05, 0.999);
            UpdateTimeDilationFactor();
        }
    }

    private void DecreaseSpeed_Click(object sender, EventArgs e)
    {
        lock (_lockB)
        {
            _velocityFraction = Math.Max(_velocityFraction - 0.05, 0.0);
            UpdateTimeDilationFactor();
        }
    }

    private void DisplayMidnightReset(string counter)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                timeLabel.Text = $"*** MIDNIGHT RESET (Counter {counter}) *** Time: {DateTime.Now:HH:mm:ss.fff}";
                timeLabel.ForeColor = Color.Yellow;
                timeLabel.BackColor = Color.DarkRed;
            }));
        }

        // Clear the midnight reset message after 5 seconds
        var resetTimer = new System.Timers.Timer(5000);
        resetTimer.Elapsed += (s, args) =>
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    timeLabel.Text = "";
                    timeLabel.BackColor = Color.Transparent;
                }));
            }
            resetTimer.Dispose();
        };
        resetTimer.Start();
    }
    private void ExitButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Clean up both timers when the form closes
        _timerA?.Stop();
        _timerA?.Dispose();
        _timerB?.Stop();
        _timerB?.Dispose();
    }
}