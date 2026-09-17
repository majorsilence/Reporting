#if WINDOWS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EncryptionProvider.Properties;

namespace EncryptionProvider
{
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form();
            prompt.Width = 500;
            prompt.Height = 200;
            prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
            prompt.Text = caption;

            Label textLabel = new Label() {Left = 50, Top = 20, Width = 400, Height = 110, Text = text};
            TextBox textBox = new TextBox() {Left = 50, Top = 100, Width = 400};
            Button confirmation = new Button()
            {
                Text = Resources.Prompt_ShowDialog_OK,
                Left = 350,
                Width = 100,
                Top = 120
            };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.ShowDialog();
            prompt.TopMost = true;


            Screen screen = Screen.FromControl(prompt);

            Rectangle workingArea = screen.WorkingArea;
            prompt.Location = new Point()
            {
                X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - prompt.Width) / 2),
                Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - prompt.Height) / 3)
            };
            prompt.StartPosition = FormStartPosition.Manual;



            return textBox.Text;
        }



    }
}
#endif
