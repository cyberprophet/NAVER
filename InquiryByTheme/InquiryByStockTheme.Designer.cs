using System.ComponentModel;
using System.Security.Cryptography.Xml;

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
            if (strip is not null)
            {
                strip.ItemClicked -= StripItemClicked;
                strip.Dispose();
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
        strip = new ContextMenuStrip(components);
        reference = new ToolStripMenuItem();
        exit = new ToolStripMenuItem();
        timer = new System.Windows.Forms.Timer(components);
        strip.SuspendLayout();
        SuspendLayout();
        // 
        // strip
        // 
        strip.AllowMerge = false;
        strip.AutoSize = false;
        strip.DropShadowEnabled = false;
        strip.Items.AddRange(new ToolStripItem[]
        {
            reference,
            exit
        });
        strip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
        strip.Name = "strip";
        strip.RenderMode = ToolStripRenderMode.System;
        strip.ShowImageMargin = false;
        strip.ShowItemToolTips = false;
        strip.Size = new Size(48, 47);
        strip.ItemClicked += StripItemClicked;
        // 
        // reference
        // 
        reference.Name = "reference";
        reference.Size = new Size(73, 22);
        reference.Text = "연결";
        // 
        // exit
        // 
        exit.Name = "exit";
        exit.Size = new Size(73, 22);
        exit.Text = "종료";
        // 
        // notifyIcon
        // 
        notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        notifyIcon.ContextMenuStrip = strip;
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
        strip.ResumeLayout(false);
        ResumeLayout(false);
        //
        // webView
        //
        _ = webView.EnsureCoreWebView2Async();

        Controls.Add(webView);

        webView.Dock = DockStyle.Fill;
    }
    ContextMenuStrip strip;
    ToolStripMenuItem exit;
    ToolStripMenuItem reference;
    ComponentResourceManager resources;
    NotifyIcon notifyIcon;
    System.Windows.Forms.Timer timer;
    System.ComponentModel.IContainer components;
}