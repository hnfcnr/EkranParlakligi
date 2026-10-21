using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EkranParlakligi
{
    internal static class Win32
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect { public int left, top, right, bottom; }

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        public static int GetBrightness()
        {
            int val = 100;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
            {
                uint num = 0;
                if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out num) && num > 0)
                {
                    PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[num];
                    if (GetPhysicalMonitorsFromHMONITOR(hMonitor, num, monitors))
                    {
                        foreach (var m in monitors)
                        {
                            uint min = 0, cur = 0, max = 0;
                            if (GetMonitorBrightness(m.hPhysicalMonitor, out min, out cur, out max))
                            {
                                val = (int)cur;
                                break;
                            }
                        }
                        DestroyPhysicalMonitors(num, monitors);
                    }
                }
                return false;
            }, IntPtr.Zero);
            return val;
        }

        public static int SetBrightness(uint target)
        {
            int updated = 0;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
            {
                uint num = 0;
                if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out num) && num > 0)
                {
                    PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[num];
                    if (GetPhysicalMonitorsFromHMONITOR(hMonitor, num, monitors))
                    {
                        foreach (var m in monitors)
                        {
                            uint min = 0, cur = 0, max = 0;
                            if (GetMonitorBrightness(m.hPhysicalMonitor, out min, out cur, out max))
                            {
                                uint safeTarget = Math.Min(max, Math.Max(min, target));
                                if (SetMonitorBrightness(m.hPhysicalMonitor, safeTarget))
                                {
                                    updated++;
                                }
                            }
                        }
                        DestroyPhysicalMonitors(num, monitors);
                    }
                }
                return true;
            }, IntPtr.Zero);
            return updated;
        }
    }

    public class TrayBrightnessApp : Form
    {
        private const string MsgName = "EkranParlakligi_ShowWindowMsg_v1";
        private readonly uint wmShow;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Label lblTitle;
        private Label lblSub;
        private Label lblPercent;
        private TrackBar trackBar;
        private Button[] presetButtons;
        private Button btnHide;
        private System.Windows.Forms.Timer debounceTimer;
        private uint pendingBrightness = 100;
        private bool allowShow = false;
        private bool allowExit = false;

        public TrayBrightnessApp()
        {
            wmShow = Win32.RegisterWindowMessage(MsgName);
            InitializeComponents();
            InitializeTray();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!allowShow) value = false;
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)wmShow)
            {
                ShowFlyout();
            }
            base.WndProc(ref m);
        }

        private void InitializeComponents()
        {
            this.Text = "Ekran Parlaklığı";
            this.Size = new Size(390, 325);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(24, 25, 38);
            this.ForeColor = Color.FromArgb(205, 214, 244);
            this.DoubleBuffered = true;

            debounceTimer = new System.Windows.Forms.Timer();
            debounceTimer.Interval = 40;
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                Win32.SetBrightness(pendingBrightness);
                UpdateTrayIconAndTooltip((int)pendingBrightness);
            };

            // Header Container Panel
            Panel headerPanel = new Panel();
            headerPanel.Location = new Point(14, 12);
            headerPanel.Size = new Size(362, 46);
            headerPanel.BackColor = Color.FromArgb(30, 32, 48);

            lblTitle = new Label();
            lblTitle.Text = "Ekran Parlaklığı";
            lblTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(137, 180, 250);
            lblTitle.Location = new Point(12, 5);
            lblTitle.AutoSize = true;
            headerPanel.Controls.Add(lblTitle);

            lblSub = new Label();
            lblSub.Text = "Harici Monitör DDC/CI Denetimi";
            lblSub.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblSub.ForeColor = Color.FromArgb(147, 154, 183);
            lblSub.Location = new Point(12, 25);
            lblSub.AutoSize = true;
            headerPanel.Controls.Add(lblSub);

            btnHide = new Button();
            btnHide.Text = "✕";
            btnHide.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnHide.Size = new Size(28, 28);
            btnHide.Location = new Point(324, 8);
            btnHide.FlatStyle = FlatStyle.Flat;
            btnHide.FlatAppearance.BorderSize = 0;
            btnHide.BackColor = Color.FromArgb(49, 50, 68);
            btnHide.ForeColor = Color.FromArgb(243, 139, 168);
            btnHide.Cursor = Cursors.Hand;
            btnHide.Click += (s, e) => this.Hide();
            headerPanel.Controls.Add(btnHide);

            this.Controls.Add(headerPanel);

            // Large Percent Label
            lblPercent = new Label();
            lblPercent.Text = "%100";
            lblPercent.Font = new Font("Segoe UI", 26f, FontStyle.Bold);
            lblPercent.ForeColor = Color.FromArgb(249, 226, 175);
            lblPercent.Location = new Point(14, 62);
            lblPercent.Size = new Size(362, 44);
            lblPercent.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblPercent);

            // Slider
            trackBar = new TrackBar();
            trackBar.Location = new Point(16, 110);
            trackBar.Size = new Size(358, 45);
            trackBar.Minimum = 0;
            trackBar.Maximum = 100;
            trackBar.TickFrequency = 10;
            trackBar.SmallChange = 5;
            trackBar.LargeChange = 10;
            trackBar.Value = 100;
            trackBar.BackColor = Color.FromArgb(24, 25, 38);
            trackBar.ValueChanged += TrackBar_ValueChanged;
            trackBar.MouseUp += (s, e) => ApplyBrightness((uint)trackBar.Value);
            trackBar.KeyUp += (s, e) => ApplyBrightness((uint)trackBar.Value);
            this.Controls.Add(trackBar);

            // Preset Buttons: %10, %25, %50, %75, %100
            int[] presetValues = new int[] { 10, 25, 50, 75, 100 };
            string[] presetLabels = new string[] { "%10", "%25", "%50", "%75", "%100" };
            presetButtons = new Button[presetValues.Length];

            int btnWidth = 66;
            int btnHeight = 36;
            int startX = 16;
            int gap = 7;
            int btnY = 162;

            for (int i = 0; i < presetValues.Length; i++)
            {
                Button btn = new Button();
                btn.Text = presetLabels[i];
                btn.Tag = presetValues[i];
                btn.Size = new Size(btnWidth, btnHeight);
                btn.Location = new Point(startX + i * (btnWidth + gap), btnY);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                btn.Cursor = Cursors.Hand;

                if (presetValues[i] == 10)
                {
                    btn.BackColor = Color.FromArgb(38, 55, 50);
                    btn.ForeColor = Color.FromArgb(166, 227, 161);
                }
                else if (presetValues[i] == 100)
                {
                    btn.BackColor = Color.FromArgb(55, 45, 40);
                    btn.ForeColor = Color.FromArgb(250, 179, 135);
                }
                else
                {
                    btn.BackColor = Color.FromArgb(40, 42, 60);
                    btn.ForeColor = Color.FromArgb(205, 214, 244);
                }

                btn.Click += (s, e) =>
                {
                    Button b = s as Button;
                    if (b != null && b.Tag is int)
                    {
                        int val = (int)b.Tag;
                        SetFormValue(val);
                        ApplyBrightness((uint)val);
                    }
                };

                presetButtons[i] = btn;
                this.Controls.Add(btn);
            }

            // Footer Information Panel
            Panel footerPanel = new Panel();
            footerPanel.Location = new Point(14, 210);
            footerPanel.Size = new Size(362, 52);
            footerPanel.BackColor = Color.FromArgb(30, 32, 48);

            Label lblInfo = new Label();
            lblInfo.Text = "Hızlı Önayarlar: %10 • %25 • %50 • %75 • %100";
            lblInfo.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            lblInfo.ForeColor = Color.FromArgb(166, 173, 200);
            lblInfo.Location = new Point(8, 7);
            lblInfo.Size = new Size(346, 18);
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            footerPanel.Controls.Add(lblInfo);

            Label lblTip = new Label();
            lblTip.Text = "Bildirim alanındaki simgeye sağ tıklayarak da hızlı seçim yapabilirsiniz.";
            lblTip.Font = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            lblTip.ForeColor = Color.FromArgb(108, 112, 134);
            lblTip.Location = new Point(8, 27);
            lblTip.Size = new Size(346, 18);
            lblTip.TextAlign = ContentAlignment.MiddleCenter;
            footerPanel.Controls.Add(lblTip);

            this.Controls.Add(footerPanel);

            // Auto-hide when user clicks outside
            this.Deactivate += (s, e) => this.Hide();
        }

        private void InitializeTray()
        {
            int current = Win32.GetBrightness();
            if (current < 0 || current > 100) current = 100;
            SetFormValue(current);

            // Context Menu
            contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = Color.FromArgb(30, 32, 48);
            contextMenu.ForeColor = Color.FromArgb(205, 214, 244);
            contextMenu.RenderMode = ToolStripRenderMode.System;

            ToolStripMenuItem miTitle = new ToolStripMenuItem("Ekran Parlaklığı: %" + current);
            miTitle.Enabled = false;
            miTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            contextMenu.Items.Add(miTitle);
            contextMenu.Items.Add(new ToolStripSeparator());

            int[] presets = new int[] { 100, 75, 50, 25, 10 };
            string[] names = new string[] {
                "Gündüz (%100)",
                "Çalışma (%75)",
                "Normal (%50)",
                "Loş (%25)",
                "Akşam (%10)"
            };

            for (int i = 0; i < presets.Length; i++)
            {
                int val = presets[i];
                string label = names[i];
                ToolStripMenuItem item = new ToolStripMenuItem(label);
                item.Click += (s, e) =>
                {
                    SetFormValue(val);
                    Win32.SetBrightness((uint)val);
                    UpdateTrayIconAndTooltip(val);
                };
                contextMenu.Items.Add(item);
            }

            contextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem miOpen = new ToolStripMenuItem("Ayarları Aç...");
            miOpen.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            miOpen.Click += (s, e) => ShowFlyout();
            contextMenu.Items.Add(miOpen);

            ToolStripMenuItem miExit = new ToolStripMenuItem("Çıkış");
            miExit.Click += (s, e) => ExitApp();
            contextMenu.Items.Add(miExit);

            // Notify Icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = GenerateTrayIcon(current);
            notifyIcon.Text = "Ekran Parlaklığı: %" + current;
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;

            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleFlyout();
                }
            };
        }

        public void SetFormValue(int val)
        {
            int safe = Math.Min(100, Math.Max(0, val));
            trackBar.Value = safe;
            lblPercent.Text = "%" + safe;
        }

        private void ApplyBrightness(uint val)
        {
            debounceTimer.Stop();
            Win32.SetBrightness(val);
            UpdateTrayIconAndTooltip((int)val);
        }

        private void UpdateTrayIconAndTooltip(int val)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Text = "Ekran Parlaklığı: %" + val;
                Icon old = notifyIcon.Icon;
                notifyIcon.Icon = GenerateTrayIcon(val);
                if (old != null) old.Dispose();

                if (contextMenu != null && contextMenu.Items.Count > 0)
                {
                    contextMenu.Items[0].Text = "Ekran Parlaklığı: %" + val;
                }
            }
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            lblPercent.Text = "%" + trackBar.Value;
            pendingBrightness = (uint)trackBar.Value;
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        public void ToggleFlyout()
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                ShowFlyout();
            }
        }

        public void ShowFlyout()
        {
            int current = Win32.GetBrightness();
            if (current >= 0 && current <= 100)
            {
                SetFormValue(current);
            }

            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = workingArea.Right - this.Width - 12;
            int y = workingArea.Bottom - this.Height - 12;
            this.Location = new Point(x, y);

            allowShow = true;
            this.Show();
            this.Activate();
        }

        private void ExitApp()
        {
            allowExit = true;
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            Application.Exit();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(69, 71, 90), 1.5f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private Icon GenerateTrayIcon(int brightness)
        {
            int size = 32;
            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                int cx = size / 2;
                int cy = size / 2;
                int radius = 7;

                Color sunColor;
                if (brightness <= 20)
                {
                    sunColor = Color.FromArgb(166, 227, 161);
                }
                else if (brightness >= 80)
                {
                    sunColor = Color.FromArgb(250, 179, 135);
                }
                else
                {
                    sunColor = Color.FromArgb(249, 226, 175);
                }

                using (Pen rayPen = new Pen(sunColor, 2f))
                {
                    rayPen.StartCap = LineCap.Round;
                    rayPen.EndCap = LineCap.Round;

                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4.0;
                        int r1 = radius + 3;
                        int r2 = radius + 6;
                        int x1 = cx + (int)Math.Round(r1 * Math.Cos(angle));
                        int y1 = cy + (int)Math.Round(r1 * Math.Sin(angle));
                        int x2 = cx + (int)Math.Round(r2 * Math.Cos(angle));
                        int y2 = cy + (int)Math.Round(r2 * Math.Sin(angle));
                        g.DrawLine(rayPen, x1, y1, x2, y2);
                    }
                }

                using (Brush brush = new SolidBrush(sunColor))
                {
                    g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew = true;
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(true, "EkranParlakligiAppMutex_v1", out createdNew);
            }
            catch
            {
                createdNew = true;
            }

            uint wmMsg = Win32.RegisterWindowMessage(MsgName);

            if (!createdNew)
            {
                // App already running! Broadcast message to bring its flyout to front.
                Win32.PostMessage((IntPtr)0xffff, wmMsg, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayBrightnessApp());
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EkranParlakligi", "logs");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    File.WriteAllText(Path.Combine(logDir, "crash.log"), ex.ToString());
                }
                catch { }
            }
        }
    }
}
