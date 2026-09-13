using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace SampleApp2_SetData
{
    public class Form1 : Form
    {
        private Majorsilence.Reporting.RdlViewer.RdlViewer rdlViewer1;
        private Majorsilence.Reporting.RdlViewer.ViewerToolstrip reportStrip;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeViewer()
        {
            this.rdlViewer1 = new Majorsilence.Reporting.RdlViewer.RdlViewer();
            this.rdlViewer1.Anchor = ((Majorsilence.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));

            this.rdlViewer1.Location = new System.Drawing.Point(40, 69);
            this.rdlViewer1.Name = "rdlViewer1";

            this.rdlViewer1.Size = new System.Drawing.Size(731, 381);
        }

        private void InitializeComponent()
        {
            InitializeViewer();

            reportStrip = new Majorsilence.Reporting.RdlViewer.ViewerToolstrip(rdlViewer1);
            //reportStrip.Location = new Point(0, 0);
            this.Controls.Add(reportStrip);

            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = (Majorsilence.Forms.AutoScaleMode)System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(852, 462);
            this.Controls.Add(this.rdlViewer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
        }
    }
}
