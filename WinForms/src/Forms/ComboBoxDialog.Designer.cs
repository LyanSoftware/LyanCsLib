namespace Lytec.WinForms
{
    partial class ComboBoxDialog
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
            this.MainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ButtonsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ButtonOk = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.TipLabel = new System.Windows.Forms.Label();
            this.ComboBox = new Lytec.WinForms.ComboBoxEx();
            this.MainLayout.SuspendLayout();
            this.ButtonsLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 
            this.MainLayout.ColumnCount = 1;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.Controls.Add(this.ButtonsLayout, 0, 2);
            this.MainLayout.Controls.Add(this.TipLabel, 0, 0);
            this.MainLayout.Controls.Add(this.ComboBox, 0, 1);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.Padding = new System.Windows.Forms.Padding(12);
            this.MainLayout.RowCount = 3;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayout.Size = new System.Drawing.Size(365, 148);
            this.MainLayout.TabIndex = 0;
            // 
            // ButtonsLayout
            // 
            this.ButtonsLayout.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ButtonsLayout.ColumnCount = 2;
            this.ButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ButtonsLayout.Controls.Add(this.ButtonOk, 0, 0);
            this.ButtonsLayout.Controls.Add(this.ButtonCancel, 1, 0);
            this.ButtonsLayout.Location = new System.Drawing.Point(82, 99);
            this.ButtonsLayout.Name = "ButtonsLayout";
            this.ButtonsLayout.RowCount = 1;
            this.ButtonsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ButtonsLayout.Size = new System.Drawing.Size(200, 34);
            this.ButtonsLayout.TabIndex = 0;
            // 
            // ButtonOk
            // 
            this.ButtonOk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ButtonOk.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ButtonOk.Location = new System.Drawing.Point(12, 5);
            this.ButtonOk.Name = "ButtonOk";
            this.ButtonOk.Size = new System.Drawing.Size(75, 23);
            this.ButtonOk.TabIndex = 0;
            this.ButtonOk.Text = "OK";
            this.ButtonOk.UseVisualStyleBackColor = true;
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ButtonCancel.Location = new System.Drawing.Point(112, 5);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 0;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            // 
            // TipLabel
            // 
            this.TipLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.TipLabel.AutoSize = true;
            this.TipLabel.Location = new System.Drawing.Point(162, 30);
            this.TipLabel.Name = "TipLabel";
            this.TipLabel.Size = new System.Drawing.Size(41, 12);
            this.TipLabel.TabIndex = 1;
            this.TipLabel.Text = "label1";
            // 
            // ComboBox
            // 
            this.ComboBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ComboBox.FormattingEnabled = true;
            this.ComboBox.Location = new System.Drawing.Point(29, 68);
            this.ComboBox.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this.ComboBox.Name = "ComboBox";
            this.ComboBox.Size = new System.Drawing.Size(306, 20);
            this.ComboBox.TabIndex = 2;
            this.ComboBox.TextValidator = null;
            // 
            // ComboBoxDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 148);
            this.Controls.Add(this.MainLayout);
            this.Name = "ComboBoxDialog";
            this.Text = "Select";
            this.MainLayout.ResumeLayout(false);
            this.MainLayout.PerformLayout();
            this.ButtonsLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.TableLayoutPanel MainLayout;
        public System.Windows.Forms.TableLayoutPanel ButtonsLayout;
        public System.Windows.Forms.Button ButtonOk;
        public System.Windows.Forms.Button ButtonCancel;
        public System.Windows.Forms.Label TipLabel;
        public ComboBoxEx ComboBox;
    }
}
