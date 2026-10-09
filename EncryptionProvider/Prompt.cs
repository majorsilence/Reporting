using System.Drawing;
using EncryptionProvider.Properties;

namespace EncryptionProvider
{
    /// <summary>
    /// Passkey-entry dialog for encrypted RDL files. Built on Majorsilence.Forms, so it works on
    /// every platform the viewer and designer do.
    /// </summary>
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            using var prompt = new Majorsilence.Forms.Form
            {
                // Form has no separate Width/Height ints, only a settable Size.
                Size = new Size(500, 200),
                FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog,
                Text = caption,
            };
            var textLabel = new Majorsilence.Forms.Label { Left = 50, Top = 20, Width = 400, Height = 60, Text = text };
            var textBox = new Majorsilence.Forms.TextBox { Left = 50, Top = 100, Width = 400 };
            var confirmation = new Majorsilence.Forms.Button
            {
                Text = Resources.Prompt_ShowDialog_OK,
                Left = 350,
                Width = 100,
                Top = 120,
            };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.ShowDialog();
            return textBox.Text;
        }
    }
}
