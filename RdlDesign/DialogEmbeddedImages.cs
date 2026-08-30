using System;
using Majorsilence.Forms.Drawing;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using System.Xml;
using System.IO;
using Majorsilence.Forms.Drawing.Imaging;
using System.Text.RegularExpressions;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;


namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogDataSourceRef.
    /// </summary>
    internal partial class DialogEmbeddedImages 
    {
        public DialogEmbeddedImages(DesignXmlDraw draw)
        {
            _Draw = draw;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            InitValues();
        }

        private void InitValues()
        {
            //
            // Obtain the existing DataSets info
            //
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode eiNode = _Draw.GetNamedChildNode(rNode, "EmbeddedImages");
            if (eiNode == null)
                return;
            foreach (XmlNode iNode in eiNode)
            {
                if (iNode.Name != "EmbeddedImage")
                    continue;
                XmlAttribute nAttr = iNode.Attributes["Name"];
                if (nAttr == null)	// shouldn't really happen
                    continue;

                EmbeddedImageValues eiv = new EmbeddedImageValues(nAttr.Value);
                eiv.MIMEType = _Draw.GetElementValue(iNode, "MIMEType", "image/png");
                eiv.ImageData = _Draw.GetElementValue(iNode, "ImageData", "");
                this.lbImages.Items.Add(eiv);
            }
            if (lbImages.Items.Count > 0)
                lbImages.SelectedIndex = 0;
            else
                this.bOK.Enabled = false;
        }

        public void Apply()
        {
            XmlNode rNode = _Draw.GetReportNode();
            _Draw.RemoveElement(rNode, "EmbeddedImages");	// remove old EmbeddedImages
            if (this.lbImages.Items.Count <= 0)
                return;			// nothing in list?  all done

            XmlNode eiNode = _Draw.SetElement(rNode, "EmbeddedImages", null);
            foreach (EmbeddedImageValues eiv in lbImages.Items)
            {
                if (eiv.Name == null || eiv.Name.Length <= 0)
                    continue;					// shouldn't really happen
                XmlNode iNode = _Draw.CreateElement(eiNode, "EmbeddedImage", null);

                // Create the name attribute
                _Draw.SetElementAttribute(iNode, "Name", eiv.Name);
                _Draw.SetElement(iNode, "MIMEType", eiv.MIMEType);
                _Draw.SetElement(iNode, "ImageData", eiv.ImageData);
            }
        }

        private void bPaste_Click(object sender, System.EventArgs e)
        {
            // Make sure we have an image on the clipboard
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null || !iData.GetDataPresent(DataFormats.Bitmap))
            {
                MessageBox.Show(this, Strings.DialogEmbeddedImages_ShowE_CopyImageBeforePaste, Strings.DialogEmbeddedImages_ShowE_Image);
                return;
            }

            Majorsilence.Forms.Drawing.Bitmap img = (Majorsilence.Forms.Drawing.Bitmap)iData.GetData(DataFormats.Bitmap);

            // convert the image to the png format and create a base 64	string representation
            string imagedata = GetBase64Image(img);
            img.Dispose();

            if (imagedata == null)
                return;

            EmbeddedImageValues eiv = new EmbeddedImageValues("embeddedimage");
            eiv.MIMEType = "image/png";
            eiv.ImageData = imagedata;
            int cur = this.lbImages.Items.Add(eiv);

            lbImages.SelectedIndex = cur;

            this.tbEIName.Focus();
        }

        private async void bImport_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.DialogEmbeddedImages_bImport_Click_ImageFilesFilter;
            ofd.FilterIndex = 6;
            ofd.CheckFileExists = true;
            ofd.Multiselect = true;
            try
            {
                if (await ofd.ShowDialogAsync(this) != DialogResult.OK)
                    return;

                // need to create a new embedded image(s)
                int cur = 0;
                foreach (string filename in ofd.FileNames)
                {
                    Stream strm = null;
                    Majorsilence.Forms.Drawing.Image im = null;
                    string imagedata = null;
                    try
                    {
                        strm = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                        im = Majorsilence.Forms.Drawing.Image.FromStream(strm);
                        imagedata = this.GetBase64Image(im);
                    }
                    catch (Exception ex)
                    {
						MessageBox.Show(this, ex.Message, Strings.DialogEmbeddedImages_ShowE_Image);
                    }
                    finally
                    {
                        if (strm != null)
                            strm.Close();
                        if (im != null)
                            im.Dispose();
                    }

                    if (imagedata != null)
                    {
                        FileInfo fi = new FileInfo(filename);

                        string fname;
                        int offset = fi.Name.IndexOf('.');
                        if (offset >= 0)
                            fname = fi.Name.Substring(0, offset);
                        else
                            fname = fi.Name;

                        if (!ReportNames.IsNameValid(fname))
                            fname = "embeddedimage";
                        // Now check to see if we already have one with that name
                        int index = 1;
                        bool bDup = true;
                        while (bDup)
                        {
                            bDup = false;
                            foreach (EmbeddedImageValues ev in lbImages.Items)
                            {
                                if (fname == ev.Name)
                                {
                                    bDup = true;
                                    break;
                                }
                            }
                            if (bDup)
                            {	// we have a duplicate name; try adding an index number
                                fname = Regex.Replace(fname, "[0-9]*", "");		// remove old numbers (side effect removes all numbers)
                                fname += index.ToString();
                                index++;
                            }
                        }

                        EmbeddedImageValues eiv = new EmbeddedImageValues(fname);
                        eiv.MIMEType = "image/png";
                        eiv.ImageData = imagedata;
                        cur = this.lbImages.Items.Add(eiv);
                    }
                }
                lbImages.SelectedIndex = cur;
            }
            finally
            {
                ofd.Dispose();
            }
            this.tbEIName.Focus();
        }

        private void bRemove_Click(object sender, System.EventArgs e)
        {
            int cur = lbImages.SelectedIndex;
            if (cur < 0)
                return;
            lbImages.Items.RemoveAt(cur);
            if (lbImages.Items.Count <= 0)
                return;
            cur--;
            if (cur < 0)
                cur = 0;
            lbImages.SelectedIndex = cur;
        }

        private void lbImages_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int cur = lbImages.SelectedIndex;
            if (cur < 0)
                return;

            EmbeddedImageValues eiv = lbImages.Items[cur] as EmbeddedImageValues;
            if (eiv == null)
                return;

            tbEIName.Text = eiv.Name;
            lbMIMEType.Text = eiv.MIMEType;
            this.pictureImage.Image = GetImage(eiv.ImageData);
        }

        private Image GetImage(string imdata)
        {
            byte[] ba = Convert.FromBase64String(imdata);

            Stream strm = null;
            Majorsilence.Forms.Drawing.Image im = null;
            try
            {
                strm = new MemoryStream(ba);
                im = Majorsilence.Forms.Drawing.Image.FromStream(strm);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, Strings.DialogEmbeddedImages_ShowE_ErrorConvertingImage);
            }
            finally
            {
                if (strm != null)
                    strm.Close();
            }
            return im;
        }

        private string GetBase64Image(Image img)
        {
            string imagedata = null;
            try
            {
                MemoryStream ostrm = new MemoryStream();
                ImageFormat imf = ImageFormat.Png;
                img.Save(ostrm, imf);
                byte[] ba = ostrm.ToArray();
                ostrm.Close();
                imagedata = Convert.ToBase64String(ba);
            }
            catch (Exception ex)
            {
				MessageBox.Show(this, ex.Message, Strings.DialogEmbeddedImages_ShowE_Image);
                imagedata = null;
            }
            return imagedata;
        }

        private void tbEIName_TextChanged(object sender, System.EventArgs e)
        {
            int cur = lbImages.SelectedIndex;
            if (cur < 0)
                return;

            EmbeddedImageValues eiv = lbImages.Items[cur] as EmbeddedImageValues;
            if (eiv == null)
                return;

            if (eiv.Name == tbEIName.Text)
                return;

            eiv.Name = tbEIName.Text;
            // text doesn't change in listbox; force change by removing and re-adding item
            lbImages.BeginUpdate();
            lbImages.Items.RemoveAt(cur);
            lbImages.Items.Insert(cur, eiv);
            lbImages.SelectedIndex = cur;
            lbImages.EndUpdate();

        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            // Verify there are no duplicate names in the embedded list
            Hashtable ht = new Hashtable(lbImages.Items.Count + 1);
            foreach (EmbeddedImageValues eiv in lbImages.Items)
            {
                if (eiv.Name == null || eiv.Name.Length == 0)
                {
					MessageBox.Show(this, Strings.DialogEmbeddedImages_ShowE_NameMustSpecified, Strings.DialogEmbeddedImages_ShowE_Image);
                    return;
                }

                if (!ReportNames.IsNameValid(eiv.Name))
                {
                    MessageBox.Show(this,
						string.Format(Strings.DialogEmbeddedImages_ShowE_NameInvalid, eiv.Name), Strings.DialogEmbeddedImages_ShowE_Image);
                    return;
                }

                string name = (string)ht[eiv.Name];
                if (name != null)
                {
                    MessageBox.Show(this,
						string.Format(Strings.DialogEmbeddedImages_ShowE_ImageMustUniqueName, eiv.Name), Strings.DialogEmbeddedImages_ShowE_Image);
                    return;
                }
                ht.Add(eiv.Name, eiv.Name);
            }

            Apply();
            DialogResult = DialogResult.OK;
        }

        private void tbEIName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ReportNames.IsNameValid(tbEIName.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this,
					string.Format(Strings.DialogEmbeddedImages_ShowE_NameInvalid, tbEIName.Text), Strings.DialogEmbeddedImages_ShowE_Image);
            }
        }
    }

    class EmbeddedImageValues
    {
        string _Name;
        string _ImageData;		// the embedded image value
        string _MIMEType;

        public EmbeddedImageValues(string name)
        {
            _Name = name;
        }

        internal string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        internal string ImageData
        {
            get { return _ImageData; }
            set { _ImageData = value; }
        }

        internal string MIMEType
        {
            get { return _MIMEType; }
            set { _MIMEType = value; }
        }

        override public string ToString()
        {
            return _Name;
        }
    }
}
