using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogEmbeddedImages : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		DesignXmlDraw _Draw;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Button bRemove;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.Label lDataProvider;
private Majorsilence.Forms.ListBox lbImages;
private Majorsilence.Forms.Button bImport;
private Majorsilence.Forms.TextBox tbEIName;
private Majorsilence.Forms.Button bPaste;
private Majorsilence.Forms.PictureBox pictureImage;
private Majorsilence.Forms.Label lbMIMEType;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogEmbeddedImages));
			this.lDataProvider = new Majorsilence.Forms.Label();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.lbImages = new Majorsilence.Forms.ListBox();
			this.bRemove = new Majorsilence.Forms.Button();
			this.bImport = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbEIName = new Majorsilence.Forms.TextBox();
			this.bPaste = new Majorsilence.Forms.Button();
			this.lbMIMEType = new Majorsilence.Forms.Label();
			this.pictureImage = new Majorsilence.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureImage)).BeginInit();
			this.SuspendLayout();
			// 
			// lDataProvider
			// 
			resources.ApplyResources(this.lDataProvider, "lDataProvider");
			this.lDataProvider.Name = "lDataProvider";
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.Name = "bOK";
			this.bOK.Click += this.bOK_Click;
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.CausesValidation = false;
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// lbImages
			// 
			resources.ApplyResources(this.lbImages, "lbImages");
			this.lbImages.Name = "lbImages";
			this.lbImages.SelectedIndexChanged += this.lbImages_SelectedIndexChanged;
			// 
			// bRemove
			// 
			resources.ApplyResources(this.bRemove, "bRemove");
			this.bRemove.Name = "bRemove";
			this.bRemove.Click += this.bRemove_Click;
			// 
			// bImport
			// 
			resources.ApplyResources(this.bImport, "bImport");
			this.bImport.Name = "bImport";
			this.bImport.Click += this.bImport_Click;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbEIName
			// 
			resources.ApplyResources(this.tbEIName, "tbEIName");
			this.tbEIName.Name = "tbEIName";
			this.tbEIName.TextChanged += this.tbEIName_TextChanged;
			this.tbEIName.Validating += this.tbEIName_Validating;
			// 
			// bPaste
			// 
			resources.ApplyResources(this.bPaste, "bPaste");
			this.bPaste.Name = "bPaste";
			this.bPaste.Click += this.bPaste_Click;
			// 
			// lbMIMEType
			// 
			resources.ApplyResources(this.lbMIMEType, "lbMIMEType");
			this.lbMIMEType.Name = "lbMIMEType";
			// 
			// pictureImage
			// 
			resources.ApplyResources(this.pictureImage, "pictureImage");
			this.pictureImage.Name = "pictureImage";
			this.pictureImage.TabStop = false;
			// 
			// DialogEmbeddedImages
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.lbMIMEType);
			this.Controls.Add(this.pictureImage);
			this.Controls.Add(this.bPaste);
			this.Controls.Add(this.tbEIName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bRemove);
			this.Controls.Add(this.bImport);
			this.Controls.Add(this.lbImages);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.lDataProvider);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogEmbeddedImages";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.pictureImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

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
	}
}
