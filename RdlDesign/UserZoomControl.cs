using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class UserZoomControl : UserControl
    {
        private float _MinValue = 1f;
        private float _MaxValue=10f;
        private float _Step=0.1f;
        private float ValoreCorrente=1f;
        /// <summary>
        /// 
        /// </summary>
        [Browsable(true)]
        public event EventHandler<CambiaValori> ZoomChanged;

        /// <summary>
        /// MiniZoomControl
        /// </summary>
        public UserZoomControl()
        {
            Size s;
            InitializeComponent();
            TxtZoomValue.Text=ValoreCorrente.ToString("0.0");
            s = this.Size;
            s.Width = BtnMinus.Size.Width+TxtZoomValue.Size.Width+BtnPlus.Size.Width+4;
      //      this.Size = s;  
        }

        /// <summary>
        /// Setta
        /// </summary>
        /// <returns></returns>
        [Browsable(true)]
        [Category("Added")]
        [Description("Massimo Valore di zoom")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float MinValue
        {
            get  { return _MinValue; }
            set{
                if (value < 0f) { _MinValue = 0f; }
                else _MinValue = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Browsable(true)]
        [Category("Added")]
        [Description("Massimo zoom")] 
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float MaxValue
        {
            get { return _MaxValue; }
            set
            {
                if (value > 100f) { _MinValue = 100f; }
                else _MinValue = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Browsable(true)]
        [Category("Added")]
        [Description("Step zoom")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Step
        {
            get { return _Step; }
            set {   _Step = value;}
        }



        /// <summary>
        /// 
        /// </summary>
        /// 
        //[Browsable(true)]
        //[Category("Added")]
        //[Description("Cambio di Valore di zoom")]
        protected void FireValueChanged()
        {
            CambiaValori e = new CambiaValori
            {
                ValoreZoom = ValoreCorrente,
                MinZoom = _MinValue,
                MaxZoom = _MaxValue
            };
            TxtZoomValue.Text=ValoreCorrente.ToString("0.0");
            //bubble the event up to the parent
            if (this.ZoomChanged != null)
                this.ZoomChanged(this, e);
            
        }

        

        private void BtnMinus_Click(object sender, EventArgs e)
        {
            if ((ValoreCorrente - _Step) >= MinValue)
            { ValoreCorrente -= Step;
                FireValueChanged();
            }
        }
        private void BtnPlus_Click(object sender, EventArgs e)
        {
            if ((ValoreCorrente + _Step) <= MaxValue)
            {
                ValoreCorrente += Step;
                FireValueChanged();
            }
        }

        /// <summary>
        /// Classe per determinare i parametri passati a un item della classe event args
        /// </summary>
        public class CambiaValori : EventArgs
        {
            public float ValoreZoom { get; set; }
            public float MinZoom { get; set; }
            public float MaxZoom { get; set; }
        }

        
    }
}
