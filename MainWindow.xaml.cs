using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using GeographyTools;
//using Windows.Devices.Geolocation;

namespace Assignment3
{
    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        private FontFamily mainFont = new FontFamily("Constantia");

        // Some GUI elements that we need to access in multiple methods.
        private ComboBox cityComboBox;
        private ListBox cinemaListBox;
        private StackPanel screeningPanel;
        private StackPanel ticketPanel;

        public class Screening
        {
            public int ID { get; set; }
            [Column(TypeName = "time(0)")]
            public TimeSpan Time { get; set; }
            
            public int MovieID { get; set; }
            public Movie Movie { get; set; }
            public Cinema Cinema { get; set; }
            public int CinemaID { get; set; }
        }

        [Index(nameof(Name), IsUnique = true)]
        public class Cinema
        {
            public int ID { get; set; }
            [MaxLength(255)]
            public string Name { get; set; }
            [MaxLength(255)]
            public string City { get; set; }
        }

        public class Ticket
        {
            public int ID { get; set; }
            [Column(TypeName = "datetime")]
            public DateTime TimePurchased { get; set; }

            public int ScreeningID { get; set; }
            public Screening Screening { get; set; }
        }

        public class Movie
        {
            public int ID { get; set; }
            [MaxLength(255)]
            public string Title { get; set; }
            public Int16 Runtime { get; set; }
            [Column(TypeName = "date")]
            public DateTime ReleaseDate { get; set; }
            [MaxLength(255)]
            public string PosterPath { get; set; }
        }

        public class AppDbContext : DbContext
        {
            public DbSet<Screening> Screenings { get; set; }
            public DbSet<Cinema> Cinemas { get; set; }
            public DbSet<Ticket> Tickets { get; set; }
            public DbSet<Movie> Movies { get; set; }
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(
                @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=DataAccessGUIAssignment;Integrated Security=True");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            // Window options
            Title = "Cinemania";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Black;

            // Main grid
            var grid = new Grid();
            Content = grid;
            grid.Margin = spacing;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            AddToGrid(grid, CreateCinemaGUI(), 0, 0);
            AddToGrid(grid, CreateScreeningGUI(), 0, 1);
            AddToGrid(grid, CreateTicketGUI(), 0, 2);
        }

        // Create the cinema part of the GUI: the left column.
        private UIElement CreateCinemaGUI()
        {
            var grid = new Grid
            {
                MinWidth = 200
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Cinema",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            // Create the dropdown of cities.
            cityComboBox = new ComboBox
            {
                Margin = spacing
            };
            foreach (string city in GetCities())
            {
                cityComboBox.Items.Add(city);
            }
            cityComboBox.SelectedIndex = 0;
            AddToGrid(grid, cityComboBox, 1, 0);

            // When we select a city, update the GUI with the cinemas in the currently selected city.
            cityComboBox.SelectionChanged += (sender, e) =>
            {
                UpdateCinemaList();
            };

            // Create the dropdown of cinemas.
            cinemaListBox = new ListBox
            {
                Margin = spacing
            };
            AddToGrid(grid, cinemaListBox, 2, 0);
            UpdateCinemaList();

            // When we select a cinema, update the GUI with the screenings in the currently selected cinema.
            cinemaListBox.SelectionChanged += (sender, e) =>
            {
                UpdateScreeningList();
            };

            return grid;
        }

        // Create the screening part of the GUI: the middle column.
        private UIElement CreateScreeningGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Screening",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            screeningPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = screeningPanel;

            UpdateScreeningList();

            return grid;
        }

        // Create the ticket part of the GUI: the right column.
        private UIElement CreateTicketGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "My Tickets",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            ticketPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = ticketPanel;

            // Update the GUI with the initial list of tickets.
            UpdateTicketList();

            return grid;
        }

        // Get a list of all cities that have cinemas in them.
        private IEnumerable<string> GetCities()
        {
            var Cities = new List<string>();
            using (var database = new AppDbContext())
            {
                var cinemaCities = database.Cinemas
                    .OrderBy(c => c.City)
                    .Select(c => c.City).Distinct()
                    .AsNoTracking().ToList();

                foreach (var city in cinemaCities)
                {
                    Cities.Add(city);
                }
            }
            return Cities;
        }

        // Get a list of all cinemas in the currently selected city.
        private IEnumerable<string> GetCinemasInSelectedCity()
        {
            var Cinemas = new List<string>();
            string currentCity = (string)cityComboBox.SelectedItem;
            using (var database = new AppDbContext())
            {
                var cinemasInCity = database.Cinemas
                    .Where(c => c.City == currentCity)
                    .Select(c => c.Name)
                    .AsNoTracking().ToList();

                foreach (var cinema in cinemasInCity)
                {
                    Cinemas.Add(cinema);
                }
            }
            return Cinemas;
        }

        // Update the GUI with the cinemas in the currently selected city.
        private void UpdateCinemaList()
        {
            cinemaListBox.Items.Clear();
            foreach (string cinema in GetCinemasInSelectedCity())
            {
                cinemaListBox.Items.Add(cinema);
            }
        }

        // Update the GUI with the screenings in the currently selected cinema.
        private void UpdateScreeningList()
        {
            screeningPanel.Children.Clear();
            if (cinemaListBox.SelectedIndex == -1)
            {
                return;
            }
            string cinema = (string)cinemaListBox.SelectedItem;
            using (var database = new AppDbContext())
            {
                var screeningList = database.Screenings
                    .Include(s => s.Movie)
                    .Include(s => s.Cinema)
                    .Where(s => s.MovieID == s.Movie.ID)
                    .Where(s => s.CinemaID == s.Cinema.ID)
                    .Where(s => s.Cinema.Name == cinema)
                    .OrderBy(s => s.Time)
                    .AsNoTracking().ToList();

                foreach (var screening in screeningList)
                {
                    // Create the button that will show all the info about the screening and let us buy a ticket for it.
                    var button = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = spacing,
                        Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    screeningPanel.Children.Add(button);
                    int screeningID = screening.ID;

                    // When we click a screening, buy a ticket for it and update the GUI with the latest list of tickets.
                    button.Click += (sender, e) =>
                    {
                        BuyTicket(screeningID);
                    };

                    // The rest of this method is just creating the GUI element for the screening.
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    button.Content = grid;

                    var image = CreateImage(@"Posters\" + screening.Movie.PosterPath);
                    image.Width = 50;
                    image.Margin = spacing;
                    image.ToolTip = new ToolTip { Content = screening.Movie.Title };
                    AddToGrid(grid, image, 0, 0);
                    Grid.SetRowSpan(image, 3);

                    var time = (TimeSpan)screening.Time;
                    var timeHeading = new TextBlock
                    {
                        Text = TimeSpanToString(time),
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Yellow
                    };
                    AddToGrid(grid, timeHeading, 0, 1);

                    var titleHeading = new TextBlock
                    {
                        Text = screening.Movie.Title,
                        Margin = spacing,
                        FontFamily = mainFont,
                        FontSize = 16,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, titleHeading, 1, 1);

                    var releaseDate = screening.Movie.ReleaseDate;
                    int runtimeMinutes = screening.Movie.Runtime;
                    var runtime = TimeSpan.FromMinutes(runtimeMinutes);
                    string runtimeString = runtime.Hours + "h " + runtime.Minutes + "m";
                    var details = new TextBlock
                    {
                        Text = "📆 " + releaseDate.Year + "     ⏳ " + runtimeString,
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        Foreground = Brushes.Silver
                    };
                    AddToGrid(grid, details, 2, 1);
                }
            }
        }

        // Buy a ticket for the specified screening and update the GUI with the latest list of tickets.
        private void BuyTicket(int screeningID)
        { 
            using (var database = new AppDbContext())
            {
                var screening = database.Tickets
                    .Where(s => s.ScreeningID == screeningID)
                    .AsNoTracking().ToList();

                // First check if we already have a ticket for this screening.
                if (screening.Count == 0)
                {
                    Ticket newTicket = new Ticket(){
                        ScreeningID = screeningID,
                        TimePurchased = DateTime.Now
                    };
                    database.Tickets.Add(newTicket);
                    database.SaveChanges();
                }  
            }
            UpdateTicketList();
        }

        // Update the GUI with the latest list of tickets
        private void UpdateTicketList()
        {
            ticketPanel.Children.Clear();

            using (var database = new AppDbContext())
            {
                var ticketList = database.Tickets
                    .Include(t => t.Screening)
                    .Include(t => t.Screening.Movie)
                    .Include(t => t.Screening.Cinema)
                    .Where(t => t.ScreeningID == t.Screening.ID)
                    .Where(t => t.Screening.MovieID == t.Screening.Movie.ID)
                    .Where(t => t.Screening.CinemaID == t.Screening.Cinema.ID)
                    .OrderBy(t => t.TimePurchased)
                    .AsNoTracking().ToList();

                // For each ticket:
                foreach (var ticket in ticketList)
                {
                    // Create the button that will show all the info about the ticket and let us remove it.
                    var button = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = spacing,
                        Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    ticketPanel.Children.Add(button);
                    int ticketID = ticket.ID;

                    // When we click a ticket, remove it and update the GUI with the latest list of tickets.
                    button.Click += (sender, e) =>
                    {
                        RemoveTicket(ticketID);
                    };

                    // The rest of this method is just creating the GUI element for the screening.
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    button.Content = grid;

                    var image = CreateImage(@"Posters\" + ticket.Screening.Movie.PosterPath);
                    image.Width = 30;
                    image.Margin = spacing;
                    image.ToolTip = new ToolTip { Content = ticket.Screening.Movie.Title };
                    AddToGrid(grid, image, 0, 0);
                    Grid.SetRowSpan(image, 2);

                    var titleHeading = new TextBlock
                    {
                        Text = ticket.Screening.Movie.Title,
                        Margin = spacing,
                        FontFamily = mainFont,
                        FontSize = 14,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, titleHeading, 0, 1);

                    var time = (TimeSpan)ticket.Screening.Time;
                    string timeString = TimeSpanToString(time);
                    var timeAndCinemaHeading = new TextBlock
                    {
                        Text = timeString + " - " + ticket.Screening.Cinema.Name,
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Yellow,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, timeAndCinemaHeading, 1, 1);
                }
            }
        }

        // Remove the ticket for the specified screening and update the GUI with the latest list of tickets.
        private void RemoveTicket(int ticketID)
        {
            using (var database = new AppDbContext())
            {
                Ticket selectedTicket = database.Tickets
                    .Where(t => t.ID == ticketID)
                    .AsNoTracking().First();

                database.Tickets.Remove(selectedTicket);
                database.SaveChanges();
            }
            UpdateTicketList();
        }

        // Helper method to add a GUI element to the specified row and column in a grid.
        private void AddToGrid(Grid grid, UIElement element, int row, int column)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        // Helper method to create a high-quality image for the GUI.
        private Image CreateImage(string filePath)
        {
            ImageSource source = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
            Image image = new Image
            {
                Source = source,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        // Helper method to turn a TimeSpan object into a string, such as 2:05.
        private string TimeSpanToString(TimeSpan timeSpan)
        {
            string hourString = timeSpan.Hours.ToString().PadLeft(2, '0');
            string minuteString = timeSpan.Minutes.ToString().PadLeft(2, '0');
            string timeString = hourString + ":" + minuteString;
            return timeString;
        }
    }
}