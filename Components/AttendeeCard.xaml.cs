using Ticket.Models;

namespace Ticket.Components
{
    public partial class AttendeeCard : ContentView
    {
        public static readonly BindableProperty AttendeeProperty =
            BindableProperty.Create(nameof(Attendee), typeof(Attendee), typeof(AttendeeCard), null,
                propertyChanged: OnAttendeeChanged);

        public Attendee? Attendee
        {
            get => (Attendee?)GetValue(AttendeeProperty);
            set => SetValue(AttendeeProperty, value);
        }

        private static void OnAttendeeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue is Attendee attendee)
                ((AttendeeCard)bindable).UpdateUI(attendee);
        }

        public AttendeeCard()
        {
            InitializeComponent();
        }

        private void UpdateUI(Attendee attendee)
        {
            var initials = attendee.FullName.Length > 0
                ? string.Join("", attendee.FullName.Split(' ').Take(2).Select(w => w[0])).ToUpper()
                : "?";

            InitialsLabel.Text = initials;
            NameLabel.Text = attendee.FullName;
            TicketCodeLabel.Text = attendee.TicketCode;
            TicketTypeLabel.Text = attendee.TicketType;
            DateLabel.Text = attendee.RegisteredAt.ToString("MMM dd");

            // Show photo if available
            if (!string.IsNullOrEmpty(attendee.PhotoUrl))
            {
                PhotoImage.Source = ImageSource.FromUri(new Uri(attendee.PhotoUrl));
                PhotoImage.IsVisible = true;
                InitialsLabel.IsVisible = false;
            }
            else
            {
                PhotoImage.IsVisible = false;
                InitialsLabel.IsVisible = true;
            }

            if (attendee.IsCheckedIn)
            {
                StatusBadge.BackgroundColor = Color.FromArgb("#10B981");
                StatusLabel.FormattedText = new FormattedString
                {
                    Spans =
                    {
                        new Span { Text = "\uf00c ", FontFamily = "FAS", TextColor = Colors.White },
                        new Span { Text = "In", TextColor = Colors.White }
                    }
                };
            }
            else
            {
                StatusBadge.BackgroundColor = Color.FromArgb("#F59E0B");
                StatusLabel.Text = "Pending";
            }
        }
    }
}
