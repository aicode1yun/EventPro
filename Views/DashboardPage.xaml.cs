using Ticket.ViewModels;

namespace Ticket.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadStatsCommand.Execute(null);
            DateLabel.Text = DateTime.Now.ToString("dddd, MMM dd yyyy");
        }
    }
}
