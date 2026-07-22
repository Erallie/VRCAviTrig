using Rug.Osc;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

class Program
{
    const int MaxLogLines = 500;

    static ConcurrentDictionary<string, object> parameters = new();

    static NotifyIcon? trayIcon;
    static ToolStripMenuItem? toggleLogWindow;
    static LogWindow? logWindow;
    static bool exiting = false;

    static readonly string saveFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "saved-state.json"
    );

    class SavedParameterState
    {
        public bool TiaraOn { get; set; }
        public bool RoseOn { get; set; }
        public bool WingsOn { get; set; }
        public int Color { get; set; }
    }

    class LogWindow : Form
    {
        private readonly TextBox logTextBox;

        public LogWindow()
        {
            Text = "VRChat OSC Bridge";
            Width = 800;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };

            logTextBox.Font = new Font(
                "Consolas",
                10f
            );

            logTextBox.BackColor = Color.Black;
            logTextBox.ForeColor = Color.White;
            logTextBox.BorderStyle = BorderStyle.None;

            Controls.Add(logTextBox);
        }

        public void AppendLogLine(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLogLine), line);
                return;
            }

            logTextBox.AppendText(line + Environment.NewLine);

            int lineCount = logTextBox.GetLineFromCharIndex(
                logTextBox.TextLength
            ) + 1;

            if (lineCount > MaxLogLines)
            {
                int firstKeptCharacter = logTextBox.GetFirstCharIndexFromLine(
                    lineCount - MaxLogLines
                );

                if (firstKeptCharacter > 0)
                {
                    logTextBox.Select(0, firstKeptCharacter);
                    logTextBox.SelectedText = string.Empty;
                }
            }

            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.ScrollToCaret();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                WindowState = FormWindowState.Normal;
                UpdateLogWindowMenuText();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting)
            {
                e.Cancel = true;
                Hide();
                UpdateLogWindowMenuText();
                return;
            }

            base.OnFormClosing(e);
        }
    }

    class LogTextWriter : TextWriter
    {
        private readonly Action<string> writeLine;
        private readonly StringBuilder currentLine = new();
        private readonly object writeLock = new();

        public LogTextWriter(Action<string> writeLine)
        {
            this.writeLine = writeLine;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (writeLock)
            {
                if (value == '\r')
                {
                    return;
                }

                if (value == '\n')
                {
                    FlushCurrentLine();
                    return;
                }

                currentLine.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (value == null)
            {
                return;
            }

            foreach (char character in value)
            {
                Write(character);
            }
        }

        private void FlushCurrentLine()
        {
            string line = currentLine.ToString();
            currentLine.Clear();
            writeLine(line);
        }
    }

    static void CreateTrayIcon()
    {
        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "VRChat OSC Bridge",
            Visible = true
        };

        ContextMenuStrip menu = new ContextMenuStrip();

        toggleLogWindow =
            new ToolStripMenuItem("Show Console");

        ToolStripMenuItem exit =
            new ToolStripMenuItem("Exit");

        toggleLogWindow.Click += (sender, e) =>
        {
            if (logWindow == null)
            {
                return;
            }

            if (logWindow.Visible)
            {
                logWindow.Hide();
                UpdateLogWindowMenuText();
            }
            else
            {
                logWindow.Show();
                logWindow.Activate();
                UpdateLogWindowMenuText();
            }
        };

        exit.Click += (sender, e) =>
        {
            exiting = true;
            trayIcon.Visible = false;
            logWindow?.Close();
            Application.Exit();
        };

        menu.Items.Add(toggleLogWindow);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        trayIcon.ContextMenuStrip = menu;
    }

    static void UpdateLogWindowMenuText()
    {
        if (toggleLogWindow != null)
        {
            toggleLogWindow.Text =
                logWindow?.Visible == true
                    ? "Hide Console"
                    : "Show Console";
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        logWindow = new LogWindow();

        LogTextWriter logWriter = new LogTextWriter(
            line => logWindow.AppendLogLine(line)
        );

        Console.SetOut(logWriter);
        Console.SetError(logWriter);

        CreateTrayIcon();

        Console.WriteLine("VRChat OSC Bridge Started");

        Task.Run(ListenForVRChat);
        Task.Run(ListenForCommands);

        Application.Run();
    }

	static void ListenForVRChat()
	{
		using OscReceiver receiver = new OscReceiver(9001);

		receiver.Connect();

		Console.WriteLine("Listening for VRChat OSC...");

		while (true)
		{
			if (receiver.TryReceive(out OscPacket packet))
			{
				if (packet is OscMessage message)
				{
					if (message.Address.StartsWith("/avatar/parameters/"))
                    {
                        string parameter =
                            message.Address.Replace(
                                "/avatar/parameters/",
                                ""
                            );

                        object value = message[0];

                        if (parameter == "TiaraOn" || parameter == "Wings/ToggledOn" || parameter == "RoseOn" || parameter == "Color")
                        {
                            parameters[parameter] = value;

                            Console.WriteLine(
                                $"{parameter} = {value} ({value.GetType().Name})"
                            );
                        }
                    }
				}
			}
		}
	}
    
    static void ListenForCommands()
    {
        using UdpClient listener = new UdpClient(8765);

        Console.WriteLine(
            "Listening for commands on UDP port 8765..."
        );

        while (true)
        {
            IPEndPoint endpoint = new IPEndPoint(
                IPAddress.Any,
                8765
            );

            byte[] data = listener.Receive(
                ref endpoint
            );

            string command = Encoding.UTF8.GetString(data).Trim();

            Console.WriteLine(
                $"Command received: {command}"
            );

            string[] parts = command.Split(' ');

            if (parts[0] == "save")
            {
                SaveParameterState();
            }
            else if (parts[0] == "load")
            {
                LoadParameterState();
            }
            else if (parts.Length >= 2 && parts[0] == "toggle")
            {
                ToggleParameter(parts[1]);
            }
            else if (parts.Length >= 3 && parts[0] == "set")
            {
                SetParameter(
                    parts[1],
                    int.Parse(parts[2])
                );
            }
        }
    }


	static void ToggleParameter(string parameter)
    {
        if (!parameters.TryGetValue(parameter, out object? value))
        {
            Console.WriteLine(
                $"{parameter} has no known state."
            );

            return;
        }

        if (value is bool boolValue)
        {
            SetParameter(parameter, !boolValue);

            Console.WriteLine(
                $"{parameter}: {boolValue} -> {!boolValue}"
            );
        }
        else if (value is int intValue)
        {
            int newValue;

            do
            {
                newValue = Random.Shared.Next(1, 5);
            }
            while (newValue == intValue);

            SetParameter(parameter, newValue);

            Console.WriteLine(
                $"{parameter}: {intValue} -> {newValue}"
            );
        }
        else
        {
            Console.WriteLine(
                $"{parameter} is not a boolean or Int32."
            );
        }
    }


	static void SetParameter(string parameter, object value)
    {
        using OscSender sender = new OscSender(
            IPAddress.Parse("127.0.0.1"),
            9002,
            9000
        );

        sender.Connect();

        OscMessage message = new OscMessage(
            $"/avatar/parameters/{parameter}",
            value
        );

        sender.Send(message);

        sender.Close();

        parameters[parameter] = value;
    }

    static void SaveParameterState()
    {
        if (
            !parameters.TryGetValue("TiaraOn", out object? tiaraValue) ||
            !parameters.TryGetValue("RoseOn", out object? roseValue) ||
            !parameters.TryGetValue("Wings/ToggledOn", out object? wingsValue) ||
            !parameters.TryGetValue("Color", out object? colorValue)
        )
        {
            Console.WriteLine("Cannot save because one or more parameters have no known state.");
            return;
        }

        SavedParameterState state = new SavedParameterState
        {
            TiaraOn = (bool)tiaraValue,
            RoseOn = (bool)roseValue,
            WingsOn = (bool)wingsValue,
            Color = (int)colorValue
        };

        string json = JsonSerializer.Serialize(
            state,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(saveFilePath, json);

        Console.WriteLine("Parameter state saved.");
    }

    static void LoadParameterState()
    {
        if (!File.Exists(saveFilePath))
        {
            Console.WriteLine("No saved parameter state was found.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);

        SavedParameterState? state =
            JsonSerializer.Deserialize<SavedParameterState>(json);

        if (state == null)
        {
            Console.WriteLine("Could not read saved parameter state.");
            return;
        }

        SetParameter("TiaraOn", state.TiaraOn);
        SetParameter("RoseOn", state.RoseOn);
        SetParameter("Wings/ToggledOn", state.WingsOn);
        SetParameter("Color", state.Color);

        Console.WriteLine("Saved parameter state reapplied.");
    }

}
