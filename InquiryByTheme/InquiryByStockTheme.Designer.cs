using System.ComponentModel;

namespace ShareInvest;

partial class InquiryByStockTheme
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (timer is not null)
            {
                timer.Stop();
                timer.Tick -= TimerTick;
                timer.Dispose();
            }
            if (notifyIcon is not null)
            {
                if (notifyIcon.Icon is not null)
                {
                    notifyIcon.Icon.Dispose();
                }
                notifyIcon.Dispose();
            }
            if (Controls.Count > 0)
            {
                foreach (Control control in Controls)
                {
                    control.Dispose();
                }
                Controls.Clear();
            }
            if (components != null)
            {
                components.Dispose();

                components = null;
            }
        }
        base.Dispose(disposing);
    }
    void InitializeComponent()
    {
        components = new Container();
        resources = new ComponentResourceManager(typeof(InquiryByStockTheme));
        notifyIcon = new NotifyIcon(components);
        timer = new System.Windows.Forms.Timer(components);
        SuspendLayout();
        // 
        // notifyIcon
        // 
        notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        notifyIcon.Icon = Properties.Resources.DARK;
        notifyIcon.Text = "AnTalk";
        notifyIcon.MouseDoubleClick += (_, e) =>
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        };
        // 
        // timer
        // 
        timer.Interval = 1000;
        timer.Tick += TimerTick;
        // 
        // InquiryByStockTheme
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.Black;
        ClientSize = new Size(475, 835);
        DoubleBuffered = true;
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "InquiryByStockTheme";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "동학개미운동";
        FormClosing += JustBeforeFormClosing;
        Resize += SecuritiesResize;
        ResumeLayout(false);
        //
        // webView
        //
        _ = webView.EnsureCoreWebView2Async();

        Controls.Add(webView);

        webView.Dock = DockStyle.Fill;
    }
    ComponentResourceManager resources;
    NotifyIcon notifyIcon;
    System.Windows.Forms.Timer timer;
    System.ComponentModel.IContainer components;
}