using System;
using System.Drawing;
using System.Windows.Forms;

namespace Calculator
{
    internal sealed class InfoForm : Form
    {
        public InfoForm(string title, string heading, string body, bool monospace)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.DialogFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            ClientSize = new Size(440, monospace ? 372 : 278);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = Theme.Background;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(22, 18, 22, 16);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));

            Label headingLabel = new Label();
            headingLabel.Dock = DockStyle.Fill;
            headingLabel.Text = heading;
            headingLabel.Font = new Font(Theme.DisplaySemiboldFamily, 16f, FontStyle.Regular,
                GraphicsUnit.Point);
            headingLabel.ForeColor = Theme.Text;
            headingLabel.BackColor = Theme.Background;
            headingLabel.TextAlign = ContentAlignment.MiddleLeft;
            headingLabel.UseMnemonic = false;

            Label bodyLabel = new Label();
            bodyLabel.Dock = DockStyle.Fill;
            bodyLabel.Text = body;
            bodyLabel.Font = monospace ? Theme.DialogMonoFont : Theme.DialogFont;
            bodyLabel.ForeColor = Color.FromArgb(0xD5, 0xD5, 0xE0);
            bodyLabel.BackColor = Theme.Background;
            bodyLabel.TextAlign = ContentAlignment.TopLeft;
            bodyLabel.UseMnemonic = false;
            bodyLabel.AutoEllipsis = false;

            CalcButton close = new CalcButton();
            close.Text = "Закрыть";
            close.Style = KeyStyle.Accent;
            close.Font = Theme.DialogFont;
            close.Size = new Size(104, 34);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close.Margin = new Padding(0);
            close.DialogResult = DialogResult.OK;

            root.Controls.Add(headingLabel, 0, 0);
            root.Controls.Add(bodyLabel, 0, 1);
            root.Controls.Add(close, 0, 2);
            Controls.Add(root);

            AcceptButton = close;
            CancelButton = close;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.ApplyDarkChrome(Handle);
        }
    }
}
