using Ticket.ViewModels;

namespace Ticket.Views
{
    public partial class RegisterParticipantPage : ContentPage
    {
        private readonly RegisterParticipantViewModel _viewModel;

        public RegisterParticipantPage(RegisterParticipantViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.ResetFormCommand.Execute(null);
        }
    }
}
