using System.Drawing.Drawing2D;
using VsdDesktop.Models;
using VsdDesktop.Services;

namespace VsdDesktop
{
    public partial class MainForm : Form
    {
        private readonly ApiClient _api = new();

        // --- Color palette ---
        private readonly Color _bg = Color.FromArgb(15, 25, 35);
        private readonly Color _bgPanel = Color.FromArgb(20, 35, 50);
        private readonly Color _bgCard = Color.FromArgb(26, 40, 58);
        private readonly Color _bgInput = Color.FromArgb(10, 18, 28);
        private readonly Color _accent = Color.FromArgb(59, 130, 246);
        private readonly Color _accentHover = Color.FromArgb(96, 165, 250);
        private readonly Color _success = Color.FromArgb(52, 211, 153);
        private readonly Color _warning = Color.FromArgb(251, 191, 36);
        private readonly Color _error = Color.FromArgb(248, 113, 113);
        private readonly Color _text = Color.FromArgb(205, 214, 224);
        private readonly Color _textMuted = Color.FromArgb(100, 120, 140);
        private readonly Color _border = Color.FromArgb(30, 50, 70);
        private readonly Color _logInfo = Color.FromArgb(96, 165, 250);
        private readonly Color _logDebug = Color.FromArgb(167, 139, 250);
        private readonly Color _logWarn = Color.FromArgb(251, 191, 36);
        private readonly Color _logError = Color.FromArgb(248, 113, 113);

        // --- Layout controls ---
        private Panel _sidebar = null!;
        private Panel _mainPanel = null!;
        private Panel _headerBar = null!;
        private RichTextBox _logBox = null!;
        private Panel _statusBar = null!;
        private Label _statusLabel = null!;
        private Label _statusIcon = null!;
        private Panel _contentArea = null!;
        private Label _stepTitle = null!;
        private Label _apiEndpoint = null!;

        private Panel? _activeStep;
        private readonly Dictionary<string, Panel> _stepPanels = new();
        private readonly Dictionary<string, Button> _sidebarButtons = new();
        private readonly Dictionary<string, string> _stepStatus = new();

        private static readonly Font _fontTitle = new("Segoe UI", 13f, FontStyle.Regular);
        private static readonly Font _fontBody = new("Segoe UI", 11f, FontStyle.Regular);
        private static readonly Font _fontSmall = new("Segoe UI", 9.5f, FontStyle.Regular);
        private static readonly Font _fontMono = new("Consolas", 10f, FontStyle.Regular);
        private static readonly Font _fontMonoSm = new("Consolas", 9f, FontStyle.Regular);
        private static readonly Font _fontBold = new("Segoe UI Semibold", 11f, FontStyle.Bold);
        private static readonly Font _fontSideNav = new("Segoe UI", 10.5f, FontStyle.Regular);

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
            ShowStep("initialize");
            _ = CheckApiHealth();
        }

        private void InitializeComponent()
        {
            Text = "VSD Commissioning Suite  —  Simulation Mode";
            Size = new Size(1200, 760);
            MinimumSize = new Size(1000, 650);
            BackColor = _bg;
            ForeColor = _text;
            Font = _fontBody;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        // ─────────────────────────────── BUILD UI ────────────────────────────────

        private void BuildUI()
        {
            SuspendLayout();
            BuildTitleBar();
            BuildStatusBar();
            BuildSidebar();
            BuildMainArea();
            ResumeLayout();
        }
        private void BuildTitleBar()
        {
            var tb = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(8, 14, 22),
                Padding = new Padding(12, 0, 12, 0)
            };

            var icon = new Label { Text = "⚡", Font = new Font("Segoe UI", 14), ForeColor = _accent, AutoSize = true, Location = new Point(14, 10) };
            var title = new Label { Text = "VSD Commissioning Suite", Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = _text, AutoSize = true, Location = new Point(38, 13) };
            var mode = new Label { Text = "SIMULATION MODE", Font = new Font("Consolas", 8.5f), ForeColor = _warning, AutoSize = true };
            mode.Location = new Point(title.Location.X + 220, 16);

            _apiEndpoint = new Label
            {
                Text = "POST /initialize",
                Font = _fontMonoSm,
                ForeColor = _textMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            tb.Controls.AddRange(new Control[] { icon, title, mode, _apiEndpoint });
            tb.Resize += (s, e) => _apiEndpoint.Location = new Point(tb.Width - _apiEndpoint.Width - 16, 15);

            Controls.Add(tb);
        }

        private void BuildSidebar()
        {
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(10, 18, 28),
                Padding = new Padding(8, 10, 8, 10)
            };

            int y = 14;

            y = AddSidebarSection("WORKFLOW STEPS", y);

            var steps = new[]
            {
                ("initialize",       "1  Initialize Drive",    "⚡"),
                ("validate-config",  "2  Validate Config",     "🔧"),
                ("upload-parameters","3  Upload Params",       "📤"),
                ("motor-test",       "4  Motor Test",          "🔄"),
                ("commission",       "5  Commission",          "✅"),
            };

            foreach (var (key, label, ico) in steps)
            {
                var btn = MakeSidebarButton(ico, label, key);
                btn.Top = y;
                _sidebar.Controls.Add(btn);
                _sidebarButtons[key] = btn;
                y += 42;
            }

            y += 8;
            var sep = new Panel { Left = 10, Top = y, Width = _sidebar.Width - 20, Height = 1, BackColor = _border };
            _sidebar.Controls.Add(sep);
            y += 10;

            y = AddSidebarSection("TOOLS", y);

            var tools = new[]
            {
                ("logs",  "📋  System Logs"),
                ("health","💓  API Health"),
            };
            foreach (var (key, label) in tools)
            {
                var btn = MakeSidebarButton("", label, key);
                btn.Top = y;
                _sidebar.Controls.Add(btn);
                _sidebarButtons[key] = btn;
                y += 42;
            }

            Controls.Add(_sidebar);
        }

        private int AddSidebarSection(string text, int y)
        {
            var lbl = new Label
            {
                Text = text, Left = 12, Top = y, AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = _textMuted
            };
            _sidebar.Controls.Add(lbl);
            return y + 22;
        }

        private Button MakeSidebarButton(string icon, string text, string key)
        {
            var btn = new Button
            {
                Text = text,
                Left = 6,
                Width = _sidebar.Width - 12,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = _textMuted,
                Font = _fontSideNav,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = key
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 40, 60);
            btn.Click += (s, e) => ShowStep(key);
            return btn;
        }

        private void BuildMainArea()
        {
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _bg,
                Padding = new Padding(0)
            };

            _stepTitle = new Label
            {
                Text = "Drive Initialization",
                Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold),
                ForeColor = _text,
                Dock = DockStyle.Top,
                Height = 46,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                BackColor = Color.FromArgb(12, 22, 33)
            };

            _contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _bg,
                AutoScroll = true,
                Padding = new Padding(20, 16, 20, 16)
            };

            BuildAllStepPanels();
            BuildLogsPanel();
            BuildHealthPanel();

            _mainPanel.Controls.Add(_contentArea);
            _mainPanel.Controls.Add(_stepTitle);
            Controls.Add(_mainPanel);
        }

        private void BuildStatusBar()
        {
            _statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(8, 14, 22),
                Padding = new Padding(12, 0, 0, 0)
            };

            _statusIcon = new Label { Text = "●", ForeColor = _textMuted, AutoSize = true, Font = new Font("Segoe UI", 9), Location = new Point(12, 8) };
            _statusLabel = new Label { Text = "Ready", ForeColor = _textMuted, AutoSize = true, Font = _fontSmall, Location = new Point(28, 9) };

            var ver = new Label { Text = "VSD Suite v2.4.1-sim  |  ASP.NET Core 8", ForeColor = _textMuted, AutoSize = true, Font = _fontSmall, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            _statusBar.Resize += (s, e) => ver.Location = new Point(_statusBar.Width - ver.Width - 16, 9);

            _statusBar.Controls.AddRange(new Control[] { _statusIcon, _statusLabel, ver });
            Controls.Add(_statusBar);
        }

        // ─────────────────────────── STEP PANELS ─────────────────────────────────

        private void BuildAllStepPanels()
        {
            BuildStepPanel("initialize", "Drive Initialization", "POST /initialize",
                "Initializes the hardware bus, resolves firmware version and hardware ID.");

            BuildStepPanel("validate-config", "Configuration Validation", "POST /validate-config",
                "Validates motor nameplate parameters and configuration schema integrity.");

            BuildStepPanel("upload-parameters", "Parameter Upload", "POST /upload-parameters",
                "Writes motor parameters to drive EEPROM across multiple blocks.");

            BuildStepPanel("motor-test", "Motor Test Run", "POST /motor-test",
                "Performs ramp-up, current monitoring, THD analysis, and brake test.");

            BuildStepPanel("commission", "Final Commissioning", "POST /commission",
                "Locks configuration, runs BIST, saves commissioning record.");
        }

        private void BuildStepPanel(string key, string title, string endpoint, string description)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = _bg, Visible = false, AutoScroll = true };

            int y = 0;

            // Description card
            var descCard = MakeCard(0, y, panel.Width, 70);
            var descLbl = new Label
            {
                Text = description,
                Font = _fontSmall,
                ForeColor = _textMuted,
                Location = new Point(14, 30),
                AutoSize = false,
                Size = new Size(descCard.Width - 28, 28)
            };
            var epLbl = new Label { Text = endpoint, Font = _fontMono, ForeColor = _accent, Location = new Point(14, 10), AutoSize = true };
            descCard.Controls.AddRange(new Control[] { epLbl, descLbl });
            panel.Controls.Add(descCard);
            y += 80;

            // Run button
            var runBtn = MakeRunButton($"▶  Run {endpoint}");
            runBtn.Location = new Point(0, y);
            panel.Controls.Add(runBtn);
            y += 52;

            // Status row
            var statusCard = MakeCard(0, y, panel.Width, 44);
            var statusIcon = new Label { Text = "●", ForeColor = _textMuted, Font = new Font("Segoe UI", 10), Location = new Point(14, 13), AutoSize = true };
            var statusMsg = new Label { Text = "Ready — press Run to call the API", ForeColor = _textMuted, Font = _fontSmall, Location = new Point(32, 15), AutoSize = true };
            statusCard.Controls.AddRange(new Control[] { statusIcon, statusMsg });
            panel.Controls.Add(statusCard);
            y += 54;

            // Response area (hidden initially)
            var responseCard = MakeCard(0, y, panel.Width, 300);
            responseCard.Visible = false;

            var respTitle = new Label { Text = "API Response", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = _textMuted, Location = new Point(14, 10), AutoSize = true };
            var badgeLbl = new Label { Text = "—", Font = _fontMonoSm, ForeColor = _text, AutoSize = true, Location = new Point(120, 12) };

            var durationLbl = new Label { Text = "", Font = _fontMonoSm, ForeColor = _textMuted, AutoSize = true };
            durationLbl.Location = new Point(responseCard.Width - 120, 12);

            var dataLabel = new Label { Text = "DATA", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = _textMuted, Location = new Point(14, 32), AutoSize = true };
            var dataBox = new RichTextBox
            {
                Location = new Point(14, 50),
                Size = new Size(responseCard.Width - 28, 90),
                BackColor = _bgInput,
                ForeColor = _text,
                Font = _fontMonoSm,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var errLabel = new Label { Text = "ERRORS / WARNINGS", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = _textMuted, Location = new Point(14, 148), AutoSize = true };
            var errBox = new RichTextBox
            {
                Location = new Point(14, 165),
                Size = new Size(responseCard.Width - 28, 60),
                BackColor = _bgInput,
                ForeColor = _text,
                Font = _fontMonoSm,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var logLabel = new Label { Text = "EMBEDDED LOGS", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = _textMuted, Location = new Point(14, 232), AutoSize = true };
            _logBox = new RichTextBox
            {
                Location = new Point(14, 250),
                Size = new Size(responseCard.Width - 28, 200),
                BackColor = _bgInput,
                ForeColor = _text,
                Font = _fontMonoSm,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            responseCard.Controls.AddRange(new Control[] { respTitle, badgeLbl, durationLbl, dataLabel, dataBox, errLabel, errBox, logLabel, _logBox });
            responseCard.Height = 460;
            panel.Controls.Add(responseCard);

            // Resize logic
            panel.Resize += (s, e) =>
            {
                descCard.Width = panel.Width - 40;
                statusCard.Width = panel.Width - 40;
                responseCard.Width = panel.Width - 40;
                dataBox.Width = responseCard.Width - 28;
                errBox.Width = responseCard.Width - 28;
                _logBox.Width = responseCard.Width - 28;
                durationLbl.Location = new Point(responseCard.Width - durationLbl.Width - 14, 12);
                descLbl.Width = descCard.Width - 28;
            };

            // Run button click
            runBtn.Click += async (s, e) =>
            {
                runBtn.Enabled = false;
                statusIcon.ForeColor = _accent;
                statusMsg.ForeColor = _accent;
                statusMsg.Text = $"Calling {endpoint}...";
                SetStatus($"Calling {endpoint}...", "running");

                StepResponse result;
                try
                {
                    result = key switch
                    {
                        "initialize" => await _api.Initialize(),
                        "validate-config" => await _api.ValidateConfig(),
                        "upload-parameters" => await _api.UploadParameters(),
                        "motor-test" => await _api.MotorTest(),
                        "commission" => await _api.Commission(),
                        _ => throw new Exception("Unknown step")
                    };
                }
                catch (Exception ex)
                {
                    result = new StepResponse { Success = false, Outcome = StepOutcome.Failure, Message = ex.Message };
                }

                // Update status
                Color statusColor = result.Outcome switch
                {
                    StepOutcome.Success => _success,
                    StepOutcome.Warning => _warning,
                    _ => _error
                };
                statusIcon.ForeColor = statusColor;
                statusMsg.ForeColor = statusColor;
                statusMsg.Text = result.Outcome switch
                {
                    StepOutcome.Success => $"✓  {result.Message}",
                    StepOutcome.Warning => $"⚠  {result.Message}",
                    _ => $"✗  {result.Message}"
                };

                // Update sidebar
                _stepStatus[key] = result.Outcome.ToString();
                UpdateSidebarButton(key, result.Outcome);

                // Badge
                badgeLbl.Text = result.Outcome.ToString().ToUpper();
                badgeLbl.ForeColor = statusColor;
                durationLbl.Text = $"{result.DurationMs} ms";

                // Data box
                dataBox.Clear();
                if (result.Data?.Count > 0)
                {
                    foreach (var kv in result.Data)
                    {
                        if (kv.Value is Newtonsoft.Json.Linq.JObject jo)
                        {
                            dataBox.AppendText($"{kv.Key}:\n");
                            foreach (var prop in jo.Properties())
                                dataBox.AppendText($"  {prop.Name}: {prop.Value}\n");
                        }
                        else
                        {
                            dataBox.AppendText($"{kv.Key}: {kv.Value}\n");
                        }
                    }
                }
                else
                {
                    dataBox.AppendText("No data returned.");
                }

                // Error box
                errBox.Clear();
                if (result.Errors?.Count > 0)
                {
                    foreach (var err in result.Errors)
                    {
                        int start2 = errBox.TextLength;
                        errBox.AppendText($"[{err.Category}] {err.Code}: {err.Message}\n");
                        errBox.Select(start2, errBox.TextLength - start2);
                        errBox.SelectionColor = err.Category == ErrorCategory.ApplicationError ? _error : _warning;
                        errBox.AppendText($"  → {err.Detail}\n");
                    }
                }
                else
                {
                    errBox.SelectionColor = _success;
                    errBox.AppendText("No errors.");
                }

                // Log box
                RenderLogsToBox(_logBox, result.Logs ?? new());

                responseCard.Visible = true;
                SetStatus($"{endpoint} completed in {result.DurationMs} ms", result.Outcome == StepOutcome.Success ? "success" : result.Outcome == StepOutcome.Warning ? "warning" : "error");
                runBtn.Enabled = true;
            };

            _stepPanels[key] = panel;
            _contentArea.Controls.Add(panel);
        }

        private void BuildLogsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = _bg, Visible = false };

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = _bgCard, Padding = new Padding(10, 8, 10, 8) };
            var filterLbl = new Label { Text = "Filter:", AutoSize = true, Font = _fontSmall, ForeColor = _textMuted, Location = new Point(12, 13) };

            var filters = new[] { ("INFO", _logInfo), ("DEBUG", _logDebug), ("WARN", _logWarn), ("ERROR", _logError) };
            int fx = 58;

            var logViewBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = _bgInput,
                ForeColor = _text,
                Font = _fontMonoSm,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var activeFilters = new HashSet<string> { "INFO", "DEBUG", "WARN", "ERROR" };

            async Task RefreshLogView()
            {
                var resp = await _api.GetLogs();
                logViewBox.Clear();
                if (resp == null || resp.Logs.Count == 0)
                {
                    logViewBox.AppendText("No logs yet — run a commissioning step first.");
                    return;
                }
                foreach (var entry in resp.Logs.Where(l => activeFilters.Contains(l.Level.ToString())))
                    AppendLogLine(logViewBox, entry);
            }

            foreach (var (lvl, col) in filters)
            {
                var fb = new Button
                {
                    Text = lvl, AutoSize = false, Width = 58, Height = 26, Location = new Point(fx, 8),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(20, 30, 50), ForeColor = col,
                    Font = new Font("Consolas", 9f), Cursor = Cursors.Hand, Tag = true
                };
                fb.FlatAppearance.BorderColor = col;
                fb.FlatAppearance.BorderSize = 1;
                string lvlCopy = lvl;
                fb.Click += async (s, e) =>
                {
                    bool active = (bool)fb.Tag!;
                    fb.Tag = !active;
                    fb.BackColor = !active ? Color.FromArgb(30, 50, 80) : Color.FromArgb(20, 30, 50);
                    if (!active) activeFilters.Add(lvlCopy); else activeFilters.Remove(lvlCopy);
                    await RefreshLogView();
                };
                toolbar.Controls.Add(fb);
                fx += 64;
            }

            var refreshBtn = new Button
            {
                Text = "↻  Refresh", AutoSize = false, Width = 90, Height = 26,
                Location = new Point(fx + 10, 8), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 35, 55), ForeColor = _accent,
                Font = _fontSmall, Cursor = Cursors.Hand
            };
            refreshBtn.FlatAppearance.BorderColor = _border;
            refreshBtn.Click += async (s, e) => await RefreshLogView();

            var clearBtn = new Button
            {
                Text = "🗑  Clear", AutoSize = false, Width = 80, Height = 26,
                Location = new Point(fx + 108, 8), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 15, 15), ForeColor = _error,
                Font = _fontSmall, Cursor = Cursors.Hand
            };
            clearBtn.FlatAppearance.BorderColor = _error;
            clearBtn.Click += async (s, e) =>
            {
                if (MessageBox.Show("Clear all session logs?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await _api.GetLogs(); // triggers a refresh; clear via DELETE would need extension
                    logViewBox.Clear();
                    logViewBox.AppendText("Logs cleared.");
                }
            };

            toolbar.Controls.AddRange(new Control[] { filterLbl, refreshBtn, clearBtn });

            panel.Controls.Add(logViewBox);
            panel.Controls.Add(toolbar);

            panel.VisibleChanged += async (s, e) =>
            {
                if (panel.Visible) await RefreshLogView();
            };

            _stepPanels["logs"] = panel;
            _contentArea.Controls.Add(panel);
        }

        private void BuildHealthPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = _bg, Visible = false };

            var card = MakeCard(0, 0, 500, 240);
            card.Anchor = AnchorStyles.None;

            var title = new Label { Text = "API Health Check", Font = _fontBold, ForeColor = _text, Location = new Point(14, 12), AutoSize = true };
            var urlLbl = new Label { Text = "Endpoint: http://localhost:5000/health", Font = _fontMonoSm, ForeColor = _textMuted, Location = new Point(14, 38), AutoSize = true };

            var statusLbl = new Label { Text = "●  Unknown", Font = new Font("Segoe UI", 13), ForeColor = _textMuted, Location = new Point(14, 70), AutoSize = true };
            var pingBtn = MakeRunButton("↻  Ping API");
            pingBtn.Location = new Point(14, 110);
            pingBtn.Width = 160;

            var detail = new Label { Text = "Click Ping to check API availability", Font = _fontSmall, ForeColor = _textMuted, Location = new Point(14, 160), AutoSize = true };

            pingBtn.Click += async (s, e) =>
            {
                pingBtn.Enabled = false;
                statusLbl.Text = "●  Checking...";
                statusLbl.ForeColor = _accent;
                bool ok = await _api.CheckHealth();
                statusLbl.Text = ok ? "●  API Online" : "●  API Offline";
                statusLbl.ForeColor = ok ? _success : _error;
                detail.Text = ok
                    ? $"API responded at {DateTime.Now:HH:mm:ss}. Swagger: http://localhost:5000/swagger"
                    : "Cannot reach API. Ensure VsdApi is running (dotnet run in VsdApi folder).";
                pingBtn.Enabled = true;
            };

            card.Controls.AddRange(new Control[] { title, urlLbl, statusLbl, pingBtn, detail });
            panel.Controls.Add(card);

            panel.Resize += (s, e) =>
            {
                card.Location = new Point((panel.Width - card.Width) / 2, (panel.Height - card.Height) / 2);
            };

            _stepPanels["health"] = panel;
            _contentArea.Controls.Add(panel);
        }

        // ─────────────────────────── HELPERS ─────────────────────────────────────

        private Panel MakeCard(int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = _bgCard,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            return p;
        }

        private Button MakeRunButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 220,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 50, 90),
                ForeColor = _accentHover,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btn.FlatAppearance.BorderColor = _accent;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 65, 110);
            return btn;
        }

        private void RenderLogsToBox(RichTextBox box, List<LogEntry> logs)
        {
            box.Clear();
            foreach (var entry in logs)
                AppendLogLine(box, entry);
            box.ScrollToCaret();
        }

        private void AppendLogLine(RichTextBox box, LogEntry entry)
        {
            int start = box.TextLength;
            box.AppendText($"{entry.Timestamp}  ");
            box.Select(start, box.TextLength - start);
            box.SelectionColor = _textMuted;

            start = box.TextLength;
            string lvl = $"[{entry.Level,-5}]";
            box.AppendText(lvl + "  ");
            box.Select(start, box.TextLength - start);
            box.SelectionColor = entry.Level switch
            {
                LogLevel.INFO => _logInfo,
                LogLevel.DEBUG => _logDebug,
                LogLevel.WARN => _logWarn,
                LogLevel.ERROR => _logError,
                _ => _text
            };

            start = box.TextLength;
            box.AppendText($"[{entry.Source}]  {entry.Message}\n");
            box.Select(start, box.TextLength - start);
            box.SelectionColor = entry.Level == LogLevel.ERROR ? _logError : _text;
        }

        private void ShowStep(string key)
        {
            foreach (var kv in _stepPanels)
                kv.Value.Visible = false;

            foreach (var kv in _sidebarButtons)
            {
                kv.Value.BackColor = Color.Transparent;
                kv.Value.ForeColor = _textMuted;
            }

            if (_stepPanels.TryGetValue(key, out var panel))
            {
                panel.Visible = true;
                _activeStep = panel;
            }

            if (_sidebarButtons.TryGetValue(key, out var btn))
            {
                btn.BackColor = Color.FromArgb(25, 45, 70);
                btn.ForeColor = _accent;
            }

            var titles = new Dictionary<string, string>
            {
                ["initialize"] = "Drive Initialization",
                ["validate-config"] = "Configuration Validation",
                ["upload-parameters"] = "Parameter Upload",
                ["motor-test"] = "Motor Test Run",
                ["commission"] = "Final Commissioning",
                ["logs"] = "System Logs",
                ["health"] = "API Health"
            };

            var endpoints = new Dictionary<string, string>
            {
                ["initialize"] = "POST /initialize",
                ["validate-config"] = "POST /validate-config",
                ["upload-parameters"] = "POST /upload-parameters",
                ["motor-test"] = "POST /motor-test",
                ["commission"] = "POST /commission",
                ["logs"] = "GET /logs",
                ["health"] = "GET /health"
            };

            _stepTitle.Text = titles.GetValueOrDefault(key, key);
            _apiEndpoint.Text = endpoints.GetValueOrDefault(key, "");
        }

        private void UpdateSidebarButton(string key, StepOutcome outcome)
        {
            if (!_sidebarButtons.TryGetValue(key, out var btn)) return;
            string suffix = outcome switch
            {
                StepOutcome.Success => " ✓",
                StepOutcome.Warning => " ⚠",
                StepOutcome.Failure => " ✗",
                _ => ""
            };
            var parts = btn.Text.TrimEnd(' ', '✓', '⚠', '✗');
            btn.Text = parts + suffix;
        }

        private void SetStatus(string msg, string state)
        {
            _statusLabel.Text = msg;
            _statusIcon.ForeColor = state switch
            {
                "success" => _success,
                "warning" => _warning,
                "error" => _error,
                "running" => _accent,
                _ => _textMuted
            };
        }

        private async Task CheckApiHealth()
        {
            await Task.Delay(800);
            bool ok = await _api.CheckHealth();
            SetStatus(ok ? "API online — ready" : "⚠  API offline — start VsdApi first", ok ? "success" : "error");
        }
    }
}
