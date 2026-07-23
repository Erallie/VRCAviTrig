using Rug.Osc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

class Program
{
    const int MaxLogLines = 500;

    static readonly ConcurrentDictionary<string, object> parameters = new();
    static readonly object settingsLock = new();
    static readonly object senderLock = new();
    static OscSender? oscSender;

    static NotifyIcon? trayIcon;
    static ToolStripMenuItem? toggleWindowMenuItem;
    static MainWindow? mainWindow;
    static bool exiting;

    static readonly string settingsFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "settings.json"
    );

    static readonly string saveFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "saved-state.json"
    );

    static AppSettings settings = new();

    class AppSettings
    {
        public bool LogAllParameters { get; set; } = true;

        public HashSet<string> LoggedParameters { get; set; } = new(
            StringComparer.Ordinal
        );
        public int LocalSendPort { get; set; } = 9002;
        public int VrChatReceivePort { get; set; } = 9001;
        public int VrChatSendPort { get; set; } = 9000;
        public int CommandPort { get; set; } = 8765;

        public bool MinimizeToTray { get; set; } = true;
        public bool CloseToTray { get; set; } = true;
    }

    class MainWindow : Form
    {
        private readonly TextBox logTextBox;
        private readonly RadioButton logAllRadioButton;
        private readonly RadioButton logSelectedRadioButton;
        private readonly CheckedListBox parameterList;
        private readonly TextBox parameterNameTextBox;
        private bool refreshingParameterList;

        private readonly NumericUpDown vrChatReceivePortInput;
        private readonly NumericUpDown vrChatSendPortInput;
        private readonly NumericUpDown localSendPortInput;
        private readonly NumericUpDown commandPortInput;
        private readonly CheckBox minimizeToTrayCheckBox;
        private readonly CheckBox closeToTrayCheckBox;

        public MainWindow()
        {
            Text = "VRChat OSC Bridge";
            Width = 850;
            Height = 550;
            StartPosition = FormStartPosition.CenterScreen;

            TabControl tabs = new()
            {
                Dock = DockStyle.Fill
            };

            TabPage logTab = new("Log");
            TabPage parametersTab = new("Parameter Logging");
            TabPage appSettingsTab = new("Settings");

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10f),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            logTab.Controls.Add(logTextBox);

            TableLayoutPanel settingsLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };

            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label instructions = new()
            {
                AutoSize = true,
                Text = "Parameters are discovered automatically when VRChat sends them. " +
                    "Choose which ones should appear in the log."
            };

            logAllRadioButton = new RadioButton
            {
                AutoSize = true,
                Text = "Log all detected parameters"
            };

            logSelectedRadioButton = new RadioButton
            {
                AutoSize = true,
                Text = "Log only checked parameters"
            };

            parameterList = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Sorted = true
            };

            FlowLayoutPanel addPanel = new()
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            parameterNameTextBox = new TextBox
            {
                Width = 350,
                PlaceholderText = "Parameter name, for example Outfit/Color"
            };

            Button addButton = new()
            {
                AutoSize = true,
                Text = "Add Parameter"
            };

            addButton.Click += (_, _) => AddParameterFromTextBox();
            parameterNameTextBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddParameterFromTextBox();
                }
            };

            addPanel.Controls.Add(parameterNameTextBox);
            addPanel.Controls.Add(addButton);

            settingsLayout.Controls.Add(instructions, 0, 0);
            settingsLayout.Controls.Add(logAllRadioButton, 0, 1);
            settingsLayout.Controls.Add(logSelectedRadioButton, 0, 2);
            settingsLayout.Controls.Add(parameterList, 0, 3);
            settingsLayout.Controls.Add(addPanel, 0, 4);

            parametersTab.Controls.Add(settingsLayout);

            TableLayoutPanel appSettingsLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(20),
                AutoSize = true
            };

            appSettingsLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.AutoSize)
            );

            appSettingsLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100f)
            );

            Label portsHeading = new()
            {
                Text = "Network Ports",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };

            vrChatReceivePortInput = CreatePortInput(
                settings.VrChatReceivePort
            );

            vrChatSendPortInput = CreatePortInput(
                settings.VrChatSendPort
            );

            localSendPortInput = CreatePortInput(
                settings.LocalSendPort
            );

            commandPortInput = CreatePortInput(
                settings.CommandPort
            );

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to the system tray",
                AutoSize = true,
                Checked = settings.MinimizeToTray
            };

            closeToTrayCheckBox = new CheckBox
            {
                Text = "Close to the system tray",
                AutoSize = true,
                Checked = settings.CloseToTray
            };

            Button saveSettingsButton = new()
            {
                Text = "Save Settings",
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3)
            };

            Label restartNotice = new()
            {
                Text = "Port changes take effect after restarting the application.",
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            appSettingsLayout.Controls.Add(portsHeading, 0, 0);
            appSettingsLayout.SetColumnSpan(portsHeading, 2);

            AddSettingRow(
                appSettingsLayout,
                1,
                "VRChat receive port:",
                vrChatReceivePortInput
            );

            AddSettingRow(
                appSettingsLayout,
                2,
                "VRChat send port:",
                vrChatSendPortInput
            );

            AddSettingRow(
                appSettingsLayout,
                3,
                "Local send port:",
                localSendPortInput
            );

            AddSettingRow(
                appSettingsLayout,
                4,
                "Command port:",
                commandPortInput
            );

            appSettingsLayout.Controls.Add(
                minimizeToTrayCheckBox,
                0,
                5
            );

            appSettingsLayout.SetColumnSpan(
                minimizeToTrayCheckBox,
                2
            );

            appSettingsLayout.Controls.Add(
                closeToTrayCheckBox,
                0,
                6
            );

            appSettingsLayout.SetColumnSpan(
                closeToTrayCheckBox,
                2
            );

            FlowLayoutPanel saveSettingsPanel = new()
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 12, 0, 0)
            };

            saveSettingsPanel.Controls.Add(saveSettingsButton);
            saveSettingsPanel.Controls.Add(restartNotice);

            appSettingsLayout.Controls.Add(
                saveSettingsPanel,
                0,
                7
            );

            appSettingsLayout.SetColumnSpan(
                saveSettingsPanel,
                2
            );

            saveSettingsButton.Click += (_, _) =>
            {
                settings.VrChatReceivePort =
                    Decimal.ToInt32(vrChatReceivePortInput.Value);

                settings.VrChatSendPort =
                    Decimal.ToInt32(vrChatSendPortInput.Value);

                settings.LocalSendPort =
                    Decimal.ToInt32(localSendPortInput.Value);

                settings.CommandPort =
                    Decimal.ToInt32(commandPortInput.Value);

                settings.MinimizeToTray =
                    minimizeToTrayCheckBox.Checked;

                settings.CloseToTray =
                    closeToTrayCheckBox.Checked;

                lock (settingsLock)
                {
                    SaveSettings();
                }

                MessageBox.Show(
                    this,
                    "Settings saved. Restart the application for port changes to take effect.",
                    "Settings Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            };

            appSettingsTab.Controls.Add(appSettingsLayout);

            tabs.TabPages.Add(logTab);
            tabs.TabPages.Add(parametersTab);
            tabs.TabPages.Add(appSettingsTab);
            Controls.Add(tabs);

            logAllRadioButton.CheckedChanged += (_, _) =>
            {
                if (logAllRadioButton.Checked)
                {
                    SetLogAllParameters(true);
                }
            };

            logSelectedRadioButton.CheckedChanged += (_, _) =>
            {
                if (logSelectedRadioButton.Checked)
                {
                    SetLogAllParameters(false);
                }
            };

            parameterList.ItemCheck += (_, e) =>
            {
                if (refreshingParameterList)
                {
                    return;
                }

                string parameter = parameterList.Items[e.Index].ToString()!;
                bool enabled = e.NewValue == CheckState.Checked;

                BeginInvoke(new Action(() => SetParameterLogging(parameter, enabled)));
            };

            RefreshSettingsControls();
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

        public void AddDetectedParameter(string parameter)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AddDetectedParameter), parameter);
                return;
            }

            if (!parameterList.Items.Contains(parameter))
            {
                bool shouldBeChecked;

                lock (settingsLock)
                {
                    shouldBeChecked = settings.LoggedParameters.Contains(parameter);
                }

                parameterList.Items.Add(parameter, shouldBeChecked);
            }
        }

        private void AddParameterFromTextBox()
        {
            string parameter = parameterNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            AddDetectedParameter(parameter);
            SetParameterLogging(parameter, true);
            SetItemChecked(parameter, true);
            parameterNameTextBox.Clear();
        }

        private void SetItemChecked(string parameter, bool isChecked)
        {
            int index = parameterList.Items.IndexOf(parameter);

            if (index < 0)
            {
                return;
            }

            refreshingParameterList = true;
            parameterList.SetItemChecked(index, isChecked);
            refreshingParameterList = false;
        }

        private void RefreshSettingsControls()
        {
            refreshingParameterList = true;

            lock (settingsLock)
            {
                logAllRadioButton.Checked = settings.LogAllParameters;
                logSelectedRadioButton.Checked = !settings.LogAllParameters;

                foreach (string parameter in settings.LoggedParameters.OrderBy(
                    parameter => parameter,
                    StringComparer.Ordinal
                ))
                {
                    parameterList.Items.Add(parameter, true);
                }
            }

            refreshingParameterList = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                WindowState = FormWindowState.Normal;
                UpdateWindowMenuText();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (
                !exiting &&
                settings.CloseToTray &&
                e.CloseReason == CloseReason.UserClosing
            )
            {
                e.Cancel = true;
                Hide();
                UpdateWindowMenuText();
                return;
            }

            exiting = true;
            ShutdownApplication();

            base.OnFormClosing(e);
        }

        private static NumericUpDown CreatePortInput(int value)
        {
            return new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = Math.Clamp(value, 1, 65535),
                Width = 120
            };
        }

        private static void AddSettingRow(
            TableLayoutPanel layout,
            int row,
            string labelText,
            Control control
        )
        {
            Label label = new()
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 12, 7)
            };

            control.Anchor = AnchorStyles.Left;
            control.Margin = new Padding(0, 4, 0, 4);

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
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

        ContextMenuStrip menu = new();
        toggleWindowMenuItem = new ToolStripMenuItem("Show Window");
        ToolStripMenuItem exitMenuItem = new("Exit");

        toggleWindowMenuItem.Click += (_, _) =>
        {
            if (mainWindow == null)
            {
                return;
            }

            if (mainWindow.Visible)
            {
                mainWindow.Hide();
            }
            else
            {
                mainWindow.Show();
                mainWindow.Activate();
            }

            UpdateWindowMenuText();
        };

        exitMenuItem.Click += (_, _) =>
        {
            exiting = true;
            ShutdownApplication();
            mainWindow?.Close();
            Application.Exit();
        };

        menu.Items.Add(toggleWindowMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitMenuItem);
        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) =>
        {
            mainWindow?.Show();
            mainWindow?.Activate();
            UpdateWindowMenuText();
        };
    }

    static void UpdateWindowMenuText()
    {
        if (toggleWindowMenuItem != null)
        {
            toggleWindowMenuItem.Text = mainWindow?.Visible == true
                ? "Hide Window"
                : "Show Window";
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        LoadSettings();
        mainWindow = new MainWindow();

        LogTextWriter logWriter = new(
            line => mainWindow.AppendLogLine(line)
        );

        Console.SetOut(logWriter);
        Console.SetError(logWriter);

        CreateTrayIcon();

        oscSender = new OscSender(
            IPAddress.Loopback,
            settings.LocalSendPort,
            settings.VrChatSendPort
        );

        oscSender.Connect();

        Console.WriteLine("VRChat OSC Bridge started.");
        Console.WriteLine(
            $"Listening for commands on UDP port {settings.CommandPort}."
        );
        Console.WriteLine("Commands: toggle, random, set, save, load");

        Task.Run(ListenForVRChat);
        Task.Run(ListenForCommands);

        Application.Run();
    }

    static void ListenForVRChat()
    {
        try
        {
            using OscReceiver receiver = new(settings.VrChatReceivePort);
            receiver.Connect();

            Console.WriteLine($"Listening for VRChat OSC on port {settings.VrChatReceivePort}.");

            while (true)
            {
                if (!receiver.TryReceive(out OscPacket packet) ||
                    packet is not OscMessage message ||
                    !message.Address.StartsWith("/avatar/parameters/", StringComparison.Ordinal))
                {
                    continue;
                }

                string parameter = message.Address["/avatar/parameters/".Length..];
                object value = message[0];
                parameters[parameter] = value;
                mainWindow?.AddDetectedParameter(parameter);

                if (ShouldLogParameter(parameter))
                {
                    Console.WriteLine(
                        $"{parameter} = {FormatValue(value)} ({value.GetType().Name})"
                    );
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"VRChat OSC listener stopped: {exception.Message}");
        }
    }

    static void ListenForCommands()
    {
        try
        {
            using UdpClient listener = new(settings.CommandPort);

            while (true)
            {
                IPEndPoint endpoint = new(IPAddress.Any, settings.CommandPort);
                byte[] data = listener.Receive(ref endpoint);
                string command = Encoding.UTF8.GetString(data).Trim();

                if (string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                Console.WriteLine($"Command received: {command}");
                ExecuteCommand(command);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Command listener stopped: {exception.Message}");
        }
    }

    static void ExecuteCommand(string command)
    {
        string[] parts = command.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (parts.Length == 0)
        {
            return;
        }

        try
        {
            switch (parts[0].ToLowerInvariant())
            {
                case "toggle" when parts.Length == 2:
                    ToggleParameter(parts[1]);
                    break;

                case "random" when parts.Length == 4:
                    RandomizeParameter(parts[1], parts[2], parts[3]);
                    break;

                case "set" when parts.Length == 3:
                    SetParameterFromText(parts[1], parts[2]);
                    break;

                case "save" when parts.Length == 1:
                    SaveParameterState();
                    break;

                case "load" when parts.Length == 1:
                    LoadParameterState();
                    break;

                default:
                    Console.WriteLine("Invalid command. Use:");
                    Console.WriteLine("  toggle <parameter>");
                    Console.WriteLine("  random <parameter> <minimum> <maximum>");
                    Console.WriteLine("  set <parameter> <value>");
                    Console.WriteLine("  save");
                    Console.WriteLine("  load");
                    break;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Command failed: {exception.Message}");
        }
    }

    static void ToggleParameter(string parameter)
    {
        if (!parameters.TryGetValue(parameter, out object? currentValue))
        {
            Console.WriteLine(
                $"{parameter} has no known state. Make sure VRChat has sent it first."
            );
            return;
        }

        if (currentValue is not bool boolValue)
        {
            Console.WriteLine($"{parameter} is not a Boolean parameter.");
            return;
        }

        bool newValue = !boolValue;
        SendParameter(parameter, newValue);
        Console.WriteLine($"{parameter}: {boolValue} -> {newValue}");
    }

    static void RandomizeParameter(
        string parameter,
        string minimumText,
        string maximumText
    )
    {
        if (!parameters.TryGetValue(parameter, out object? currentValue))
        {
            Console.WriteLine(
                $"{parameter} has no known state. Make sure VRChat has sent it first."
            );
            return;
        }

        if (currentValue is int currentInt)
        {
            int minimum = int.Parse(minimumText, CultureInfo.InvariantCulture);
            int maximum = int.Parse(maximumText, CultureInfo.InvariantCulture);

            if (minimum > maximum)
            {
                throw new ArgumentException("The minimum cannot be greater than the maximum.");
            }

            long valueCount = (long)maximum - minimum + 1;

            if (valueCount == 1 && minimum == currentInt)
            {
                throw new ArgumentException(
                    "The range must contain a value different from the current value."
                );
            }

            int newValue;

            do
            {
                newValue = (int)Random.Shared.NextInt64(
                    minimum,
                    (long)maximum + 1
                );
            }
            while (newValue == currentInt);

            SendParameter(parameter, newValue);
            Console.WriteLine($"{parameter}: {currentInt} -> {newValue}");
            return;
        }

        if (currentValue is float currentFloat)
        {
            float minimum = float.Parse(minimumText, CultureInfo.InvariantCulture);
            float maximum = float.Parse(maximumText, CultureInfo.InvariantCulture);

            if (!float.IsFinite(minimum) || !float.IsFinite(maximum))
            {
                throw new ArgumentException("Float limits must be finite numbers.");
            }

            if (minimum >= maximum)
            {
                throw new ArgumentException(
                    "A float range must have a maximum greater than its minimum."
                );
            }

            float newValue;

            do
            {
                newValue = minimum + ((maximum - minimum) * Random.Shared.NextSingle());
            }
            while (newValue.Equals(currentFloat));

            SendParameter(parameter, newValue);
            Console.WriteLine(
                $"{parameter}: {FormatValue(currentFloat)} -> {FormatValue(newValue)}"
            );
            return;
        }

        Console.WriteLine($"{parameter} is not an Int32 or Single parameter.");
    }

    static void SetParameterFromText(string parameter, string valueText)
    {
        object value;

        if (parameters.TryGetValue(parameter, out object? currentValue))
        {
            value = currentValue switch
            {
                int => int.Parse(valueText, CultureInfo.InvariantCulture),
                float => float.Parse(valueText, CultureInfo.InvariantCulture),
                _ => throw new ArgumentException(
                    $"{parameter} is not an Int32 or Single parameter."
                )
            };
        }
        else if (valueText.Contains('.') ||
            valueText.IndexOf('e', StringComparison.OrdinalIgnoreCase) >= 0)
        {
            value = float.Parse(valueText, CultureInfo.InvariantCulture);
        }
        else
        {
            value = int.Parse(valueText, CultureInfo.InvariantCulture);
        }

        object? previousValue = parameters.TryGetValue(parameter, out object? oldValue)
            ? oldValue
            : null;

        SendParameter(parameter, value);

        Console.WriteLine(
            previousValue == null
                ? $"{parameter} set to {FormatValue(value)}"
                : $"{parameter}: {FormatValue(previousValue)} -> {FormatValue(value)}"
        );
    }

    static void SendParameter(string parameter, object value)
    {
        OscMessage message = new(
            $"/avatar/parameters/{parameter}",
            value
        );

        lock (senderLock)
        {
            if (oscSender == null)
            {
                throw new InvalidOperationException(
                    "The OSC sender is not connected."
                );
            }

            oscSender.Send(message);
        }

        parameters[parameter] = value;
        mainWindow?.AddDetectedParameter(parameter);
    }

    static bool ParameterValuesEqual(object first, object second)
    {
        if (first.GetType() == second.GetType())
        {
            return Equals(first, second);
        }

        if (first is int firstInt && second is float secondFloat)
        {
            return firstInt.Equals(secondFloat);
        }

        if (first is float firstFloat && second is int secondInt)
        {
            return firstFloat.Equals(secondInt);
        }

        return false;
    }

    static void SaveParameterState()
    {
        Dictionary<string, object> savedParameters = new(
            StringComparer.Ordinal
        );

        foreach (KeyValuePair<string, object> parameter in parameters)
        {
            if (
                parameter.Value is bool ||
                parameter.Value is int ||
                parameter.Value is float
            )
            {
                savedParameters[parameter.Key] = parameter.Value;
            }
        }

        string json = JsonSerializer.Serialize(
            savedParameters,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(saveFilePath, json);

        Console.WriteLine(
            $"Saved {savedParameters.Count} parameter state(s)."
        );
    }

    static void LoadParameterState()
    {
        if (!File.Exists(saveFilePath))
        {
            Console.WriteLine(
                "No saved parameter state was found."
            );

            return;
        }

        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(saveFilePath)
        );

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine(
                "The saved parameter state is not a valid JSON object."
            );

            return;
        }

        int loadedCount = 0;

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            string parameter = property.Name;

            // Compatibility with your original saved-state format.
            if (parameter == "WingsOn")
            {
                parameter = "Wings/ToggledOn";
            }

            object? value = ReadSavedParameterValue(property.Value);

            if (value == null)
            {
                Console.WriteLine(
                    $"Skipped {parameter}: unsupported saved value."
                );

                continue;
            }

            SendParameter(parameter, value);
            loadedCount++;
        }

        Console.WriteLine(
            $"Reapplied {loadedCount} saved parameter state(s)."
        );
    }

    static object? ReadSavedParameterValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                {
                    return intValue;
                }

                if (element.TryGetSingle(out float floatValue))
                {
                    return floatValue;
                }

                return null;

            default:
                return null;
        }
    }

    static bool ShouldLogParameter(string parameter)
    {
        lock (settingsLock)
        {
            return settings.LogAllParameters ||
                settings.LoggedParameters.Contains(parameter);
        }
    }

    static void SetLogAllParameters(bool logAll)
    {
        lock (settingsLock)
        {
            settings.LogAllParameters = logAll;
            SaveSettings();
        }
    }

    static void SetParameterLogging(string parameter, bool enabled)
    {
        lock (settingsLock)
        {
            if (enabled)
            {
                settings.LoggedParameters.Add(parameter);
            }
            else
            {
                settings.LoggedParameters.Remove(parameter);
            }

            SaveSettings();
        }
    }

    static void LoadSettings()
    {
        if (!File.Exists(settingsFilePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(settingsFilePath);
            AppSettings? loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

            if (loadedSettings != null)
            {
                loadedSettings.LoggedParameters = new HashSet<string>(
                    loadedSettings.LoggedParameters ?? [],
                    StringComparer.Ordinal
                );

                settings = loadedSettings;
            }
        }
        catch
        {
            settings = new AppSettings();
        }
    }

    static void SaveSettings()
    {
        string json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(settingsFilePath, json);
    }

    static string FormatValue(object value)
    {
        return value switch
        {
            float floatValue => floatValue.ToString("0.######", CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("0.######", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    static void ShutdownApplication()
    {
        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayIcon = null;
        }

        lock (senderLock)
        {
            oscSender?.Close();
            oscSender = null;
        }
    }
}
