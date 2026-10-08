using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for CodeCtl.
    /// </summary>
    internal class CodeCtl : Majorsilence.Forms.UserControl, IProperty
    {
        static internal long Counter;			// counter used for unique expression count
        private DesignXmlDraw _Draw;
        private Majorsilence.Forms.Label label1;
        private Majorsilence.Forms.Button bCheckSyntax;
        private Majorsilence.Forms.Panel panel1;
        private Majorsilence.Forms.TextBox tbCode;
		private Majorsilence.Forms.ListBox lbErrors;
        private Majorsilence.Forms.Label label2;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        internal CodeCtl(DesignXmlDraw dxDraw)
        {
            _Draw = dxDraw;
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // Initialize form using the style node values
            InitValues();			
        }

        private void InitValues()
        {
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode cNode = _Draw.GetNamedChildNode(rNode, "Code");
            tbCode.Text = "";
            if (cNode == null)
                return;

            StringReader tr = new StringReader(cNode.InnerText);
            List<string> ar = new List<string>();
            while (tr.Peek() >= 0)
            {
                string line = tr.ReadLine();
                ar.Add(line);
            }
            tr.Close();

        //    tbCode.Lines = ar.ToArray("".GetType()) as string[];
            tbCode.Lines = ar.ToArray();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(CodeCtl));
            this.DoubleBuffered = true;
			this.label1 = new Majorsilence.Forms.Label();
			this.bCheckSyntax = new Majorsilence.Forms.Button();
			this.panel1 = new Majorsilence.Forms.Panel();
			this.tbCode = new Majorsilence.Forms.TextBox();
			this.lbErrors = new Majorsilence.Forms.ListBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// bCheckSyntax
			// 
			resources.ApplyResources(this.bCheckSyntax, "bCheckSyntax");
			this.bCheckSyntax.Name = "bCheckSyntax";
			this.bCheckSyntax.Click += this.bCheckSyntax_Click;
			// 
			// panel1
			// 
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Controls.Add(this.tbCode);
			this.panel1.Controls.Add(this.lbErrors);
			this.panel1.Name = "panel1";
			// 
			// tbCode
			// 
			this.tbCode.AcceptsReturn = true;
			this.tbCode.AcceptsTab = true;
			resources.ApplyResources(this.tbCode, "tbCode");
			this.tbCode.HideSelection = false;
			this.tbCode.Name = "tbCode";
			// 
			// lbErrors
			// 
			resources.ApplyResources(this.lbErrors, "lbErrors");
			this.lbErrors.Name = "lbErrors";
			this.lbErrors.SelectedIndexChanged += this.lbErrors_SelectedIndexChanged;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// CodeCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.label2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.bCheckSyntax);
			this.Controls.Add(this.label1);
			this.Name = "CodeCtl";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

        }
        #endregion

        public bool IsValid()
        {
            return true;
        }

        public void Apply()
        {
            XmlNode rNode = _Draw.GetReportNode(); 
            if (tbCode.Text.Trim().Length > 0)
                _Draw.SetElement(rNode, "Code", tbCode.Text);
            else
                _Draw.RemoveElement(rNode, "Code");
        }

        private void bCheckSyntax_Click(object sender, System.EventArgs e)
        {
            CheckAssembly();	
        }
        
        private void CheckAssembly()
        {
            lbErrors.Items.Clear();					// clear out existing items

            // Generate the proxy source code
            List<string> lines = new List<string>();		// hold lines in array in case of error

            VBCodeProvider vbcp =  new VBCodeProvider();
            StringBuilder sb = new StringBuilder();
            //  Generate code with the following general form

            //Imports System
            //Imports Microsoft.VisualBasic
            //Imports System.Convert
            //Imports System.Math 
            //Namespace Majorsilence.Reporting.vbgen
            //Public Class MyClassn	   // where n is a uniquely generated integer
            //Sub New()
            //End Sub
            //  ' this is the code in the <Code> tag
            //End Class
            //End Namespace
            string unique = Interlocked.Increment(ref CodeCtl.Counter).ToString();
            lines.Add("Imports System");
            lines.Add("Imports Microsoft.VisualBasic");
            lines.Add("Imports System.Convert");
            lines.Add("Imports System.Math");
            lines.Add("Imports Majorsilence.Reporting.Rdl");
            lines.Add("Namespace Majorsilence.Reporting.vbgen");
            string classname = "MyClass" + unique;
            lines.Add("Public Class " + classname);
            lines.Add("Private Shared _report As CodeReport");
            lines.Add("Sub New()");
            lines.Add("End Sub");
            lines.Add("Sub New(byVal def As Report)");
            lines.Add(classname + "._report = New CodeReport(def)");
            lines.Add("End Sub");
            lines.Add("Public Shared ReadOnly Property Report As CodeReport");
            lines.Add("Get");
            lines.Add("Return " + classname + "._report");
            lines.Add("End Get");
            lines.Add("End Property");
            int pre_lines = lines.Count;            // number of lines prior to user VB code

            // Read and write code as lines
            StringReader tr = new StringReader(this.tbCode.Text);
            while (tr.Peek() >= 0)
            {
                string line = tr.ReadLine();
                lines.Add(line);
            }
            tr.Close();
            lines.Add("End Class");
            lines.Add("End Namespace");
            foreach (string l in lines)
            {
                sb.Append(l);
                sb.Append(Environment.NewLine);
            }
            string vbcode = sb.ToString();

            // Create Assembly
            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            string re = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Majorsilence.Reporting.RdlEngine.dll");  // Issue #35
            cp.ReferencedAssemblies.Add(re);

            // also allow access to classes that have been added to report
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode cNode = _Draw.GetNamedChildNode(rNode, "CodeModules");
            if (cNode != null)
            {
                foreach (XmlNode xn in cNode.ChildNodes)
                {
                    if (xn.Name != "CodeModule")
                        continue;
                    cp.ReferencedAssemblies.Add(xn.InnerText);
                }
            }

            cp.GenerateExecutable = false;
            cp.GenerateInMemory = true;
            cp.IncludeDebugInformation = false; 
            
            CompilerResults cr = vbcp.CompileAssemblyFromSource(cp, vbcode);
            if(cr.Errors.Count > 0)
            {
                StringBuilder err = new StringBuilder(string.Format("Code has {0} error(s).", cr.Errors.Count));
                foreach (CompilerError ce in cr.Errors)
                {
                    lbErrors.Items.Add(string.Format("Ln {0}- {1}", ce.Line - pre_lines, ce.ErrorText));
                }
            }
            else
                MessageBox.Show(Resources.Strings.CodeCtl_Show_NoErrors, Resources.Strings.CodeCtl_Show_CodeVerification);

            return ;
        }

        private void lbErrors_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbErrors.Items.Count == 0)
                return;
                         
            string line = lbErrors.Items[lbErrors.SelectedIndex] as string;
            if (!line.StartsWith("Ln"))
                return;

            int l = line.IndexOf('-');
            if (l < 0)
                return;
            line = line.Substring(3, l-3);
            try
            {
                int i = Convert.ToInt32(line);
                Goto(i);
            }
            catch {}		// we don't care about the error
            return;
        }

        public void Goto(int nLine)
        {	
            int offset = 0; 
            nLine = Math.Min(nLine, tbCode.Lines.Length);		// don't go off the end

            for ( int i = 0; i < nLine - 1 && i < tbCode.Lines.Length; ++i ) 
                offset += (this.tbCode.Lines[i].Length + 2); 

            Control savectl = this.ActiveControl;
            tbCode.Focus(); 
            tbCode.Select( offset, this.tbCode.Lines[nLine > 0? nLine-1: 0].Length);
            this.ActiveControl = savectl;
        }


    }
}
