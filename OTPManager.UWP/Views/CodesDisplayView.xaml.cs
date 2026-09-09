using MvvmCross.Platforms.Uap.Views;
using OTPManager.Shared.ViewModels;

namespace OTPManager.UWP.Views
{
    public sealed partial class CodesDisplayView : MvxWindowsPage
    {
        public CodesDisplayViewModel VM => ViewModel as CodesDisplayViewModel;

        public CodesDisplayView()
        {
            this.InitializeComponent();
        }

        private void ItemClicked(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
        {
            VM.ItemClicked.Execute((OTPDisplayViewModel)e.ClickedItem);
        }

        private void SearchBox_TextChanged(Windows.UI.Xaml.Controls.AutoSuggestBox sender, Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason.UserInput || args.Reason == Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
            {
                if (VM != null)
                {
                    VM.SearchText = sender.Text;
                }
            }
        }

        private void SearchBox_QuerySubmitted(Windows.UI.Xaml.Controls.AutoSuggestBox sender, Windows.UI.Xaml.Controls.AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (VM != null)
            {
                VM.SearchText = args.QueryText;
            }
        }
    }
}
