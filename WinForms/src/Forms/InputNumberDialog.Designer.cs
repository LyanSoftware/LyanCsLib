
namespace Lytec.WinForms
{
    partial class InputNumberDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.NumberInput = new WinForms.NumericUpDownEx();
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelButton1 = new System.Windows.Forms.Button();
            this.MainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.Tip = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.NumberInput)).BeginInit();
            this.MainLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // NumberInput
            // 
            this.NumberInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.NumberInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MainLayout.SetColumnSpan(this.NumberInput, 2);
            this.NumberInput.Location = new System.Drawing.Point(18, 52);
            this.NumberInput.Margin = new System.Windows.Forms.Padding(10);
            this.NumberInput.Name = "NumberInput";
            this.NumberInput.Size = new System.Drawing.Size(158, 21);
            this.NumberInput.TabIndex = 1;
            this.NumberInput.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // OKButton
            // 
            this.OKButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.OKButton.Location = new System.Drawing.Point(21, 89);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(62, 22);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            // 
            // CancelButton1
            // 
            this.CancelButton1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.CancelButton1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelButton1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.CancelButton1.Location = new System.Drawing.Point(110, 89);
            this.CancelButton1.Name = "CancelButton1";
            this.CancelButton1.Size = new System.Drawing.Size(62, 22);
            this.CancelButton1.TabIndex = 3;
            this.CancelButton1.Text = "Cancel";
            this.CancelButton1.UseVisualStyleBackColor = true;
            // 
            // MainLayout
            // 
            this.MainLayout.ColumnCount = 2;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.Controls.Add(this.NumberInput, 0, 1);
            this.MainLayout.Controls.Add(this.CancelButton1, 1, 2);
            this.MainLayout.Controls.Add(this.OKButton, 0, 2);
            this.MainLayout.Controls.Add(this.Tip, 0, 0);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.Padding = new System.Windows.Forms.Padding(8);
            this.MainLayout.RowCount = 3;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayout.Size = new System.Drawing.Size(194, 122);
            this.MainLayout.TabIndex = 0;
            // 
            // Tip
            // 
            this.Tip.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Tip.AutoSize = true;
            this.MainLayout.SetColumnSpan(this.Tip, 2);
            this.Tip.Location = new System.Drawing.Point(79, 18);
            this.Tip.Margin = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.Tip.Name = "Tip";
            this.Tip.Size = new System.Drawing.Size(35, 12);
            this.Tip.TabIndex = 0;
            this.Tip.Text = "Input";
            // 
            // InputNumberDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(194, 122);
            this.Controls.Add(this.MainLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimumSize = new System.Drawing.Size(200, 160);
            this.Name = "InputNumberDialog";
            this.Text = "Input";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.NumberInput)).EndInit();
            this.MainLayout.ResumeLayout(false);
            this.MainLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel MainLayout;
        public NumericUpDownEx NumberInput;
        public System.Windows.Forms.Button OKButton;
        public System.Windows.Forms.Button CancelButton1;
        public System.Windows.Forms.Label Tip;
    }
}
