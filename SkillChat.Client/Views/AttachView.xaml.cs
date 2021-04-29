using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views
{
    public class AttachView : UserControl
    {
        public AttachView()
        {
            
            this.InitializeComponent();
            this.FindControl<TextBox>("InputContent").Focus();
            //ViewModel.Attach.FilesDialog.AttachListEventHandler += ListChanged;
        }

        private void ListChanged(bool obj)
        {
           this.FindControl<TextBox>("InputContent").Focus();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
    }
}
