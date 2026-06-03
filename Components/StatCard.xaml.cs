namespace Ticket.Components
{
    public partial class StatCard : ContentView
    {
        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(StatCard), "\uf080",
                propertyChanged: (b, _, n) => ((StatCard)b).IconLabel.Text = (string)n);

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(string), typeof(StatCard), "0",
                propertyChanged: (b, _, n) => ((StatCard)b).ValueLabel.Text = (string)n);

        public static readonly BindableProperty CardTitleProperty =
            BindableProperty.Create(nameof(CardTitle), typeof(string), typeof(StatCard), "Title",
                propertyChanged: (b, _, n) => ((StatCard)b).TitleLabel.Text = (string)n);

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string CardTitle
        {
            get => (string)GetValue(CardTitleProperty);
            set => SetValue(CardTitleProperty, value);
        }

        public StatCard()
        {
            InitializeComponent();
        }
    }
}
