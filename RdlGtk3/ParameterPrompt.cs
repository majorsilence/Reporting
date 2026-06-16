using Gtk;

namespace Majorsilence.Reporting.RdlGtk3
{
    public partial class ParameterPrompt : Dialog
    {
        private Entry TextBoxParameters;
        private VBox vbox;
        private HBox hbox3;
        private Label label1;

        public ParameterPrompt()
        {
            Build();
        }

        private void Build()
        {
            this.Title = "Parameters";
            this.Modal = true;
            this.DefaultWidth = 400;
            this.DefaultHeight = 200;

            // Create the main vbox for content
#pragma warning disable CS0612
            vbox = new VBox(false, 6);
#pragma warning restore CS0612
            vbox.BorderWidth = 12;

            // Create label
            label1 = new Label("Parameters:");
            label1.Xalign = 0;
            vbox.PackStart(label1, false, false, 0);

            // Create text entry
            TextBoxParameters = new Entry();
            vbox.PackStart(TextBoxParameters, false, false, 0);

            // Create button box
#pragma warning disable CS0612
            hbox3 = new HBox(true, 6);
#pragma warning restore CS0612
            
            Button cancelButton = new Button("Cancel");
            cancelButton.Clicked += (sender, e) => this.Respond(ResponseType.Cancel);
            hbox3.PackStart(cancelButton, true, true, 0);

            Button okButton = new Button("OK");
            okButton.Clicked += (sender, e) => this.Respond(ResponseType.Ok);
            hbox3.PackStart(okButton, true, true, 0);

            vbox.PackStart(hbox3, false, false, 0);

            // Add the vbox to the dialog's content area
            this.ContentArea.Add(vbox);
            this.ShowAll();
        }

        public string Parameters
        {
            get => TextBoxParameters.Text;
            set => TextBoxParameters.Text = value;
        }
    }
}