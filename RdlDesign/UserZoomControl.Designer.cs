namespace Majorsilence.Reporting.RdlDesign
{
    partial class UserZoomControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DoubleBuffered = true;
            this.BtnPlus = new Majorsilence.Forms.Button();
            this.BtnMinus = new Majorsilence.Forms.Button();
            this.TxtZoomValue = new Majorsilence.Forms.TextBox();
            this.SuspendLayout();
            // 
            // BtnPlus
            // 
            this.BtnPlus.AutoSize = true;
            this.BtnPlus.Font = new Majorsilence.Forms.Drawing.Font("Segoe UI", 9F, Majorsilence.Forms.Drawing.FontStyle.Bold, Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnPlus.Location = new System.Drawing.Point(86, 1);
            this.BtnPlus.Margin = new Majorsilence.Forms.Padding(0);
            this.BtnPlus.Name = "BtnPlus";
            this.BtnPlus.Size = new System.Drawing.Size(36, 30);
            this.BtnPlus.TabIndex = 0;
            this.BtnPlus.TabStop = false;
            this.BtnPlus.Text = "+";
            this.BtnPlus.UseVisualStyleBackColor = true;
            this.BtnPlus.Click += this.BtnPlus_Click;
            // 
            // BtnMinus
            // 
            this.BtnMinus.AutoSizeMode = Majorsilence.Forms.AutoSizeMode.GrowAndShrink;
            this.BtnMinus.Font = new Majorsilence.Forms.Drawing.Font("Segoe UI", 9F, Majorsilence.Forms.Drawing.FontStyle.Bold, Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnMinus.Location = new System.Drawing.Point(2, 1);
            this.BtnMinus.Margin = new Majorsilence.Forms.Padding(0);
            this.BtnMinus.Name = "BtnMinus";
            this.BtnMinus.Size = new System.Drawing.Size(36, 30);
            this.BtnMinus.TabIndex = 1;
            this.BtnMinus.TabStop = false;
            this.BtnMinus.Text = "-";
            this.BtnMinus.UseVisualStyleBackColor = true;
            this.BtnMinus.Click += this.BtnMinus_Click;
            // 
            // TxtZoomValue
            // 
            this.TxtZoomValue.BorderStyle = Majorsilence.Forms.BorderStyle.None;
            this.TxtZoomValue.Font = new Majorsilence.Forms.Drawing.Font("Segoe UI", 9F, Majorsilence.Forms.Drawing.FontStyle.Bold, Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtZoomValue.Location = new System.Drawing.Point(38, 6);
            this.TxtZoomValue.Margin = new Majorsilence.Forms.Padding(0);
            this.TxtZoomValue.Name = "TxtZoomValue";
            this.TxtZoomValue.Size = new System.Drawing.Size(48, 20);
            this.TxtZoomValue.TabIndex = 2;
            this.TxtZoomValue.TabStop = false;
            this.TxtZoomValue.TextAlign = Majorsilence.Forms.HorizontalAlignment.Center;
            // 
            // UserZoomControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = Majorsilence.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = Majorsilence.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.TxtZoomValue);
            this.Controls.Add(this.BtnMinus);
            this.Controls.Add(this.BtnPlus);
            this.Font = new Majorsilence.Forms.Drawing.Font("Segoe UI Semibold", 9F, Majorsilence.Forms.Drawing.FontStyle.Bold, Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new Majorsilence.Forms.Padding(0);
            this.Name = "UserZoomControl";
            this.Size = new System.Drawing.Size(122, 31);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Majorsilence.Forms.Button BtnPlus;
        private Majorsilence.Forms.Button BtnMinus;
        private Majorsilence.Forms.TextBox TxtZoomValue;
    }
}
