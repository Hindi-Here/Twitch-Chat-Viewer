using Microsoft.Playwright;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using System.Text.Json;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.ComponentModel;
using System.Threading;
using System.Xml.Linq;

namespace TwitchChatView
{
    public partial class Chat : Window
    {
        readonly string config_path = System.IO.Path.Combine(@"..\..\..\config.json");
        private readonly string _link;

        private TaskbarIcon? _notifyIcon;
        private Config? config;

        public Chat(string link)
        {
            InitializeComponent();
            SetupTrayIcon();
            ChatConfig();

            _link = link;
        }

        private async void ChatLoaded(object sender, RoutedEventArgs e)
            => await ChatHandler();

        private void ChatConfig()
        {
            string json = File.ReadAllText(config_path);
            config = JsonSerializer.Deserialize<Config>(json)!;

            DefaultWindowSetting();

            SetWindowSize(config);
            SetWindowLocation(config);
            SetBackground(config);

            SetChatSize(config);
            SetChatLocation(config);
        }

        private void DefaultWindowSetting()
        {
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Transparent;
        }

        private void SetWindowSize(Config config)
        {
            Width = config.w_size_x.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? SystemParameters.WorkArea.Width
                  : int.Parse(config.w_size_x);

            Height = config.w_size_y.Equals("auto", StringComparison.OrdinalIgnoreCase)
                   ? SystemParameters.WorkArea.Height
                   : int.Parse(config.w_size_y);
        }

        private void SetChatSize(Config config)
        {
            if (config.chat_on_window)
            {
                chatScroll.Width = Width;
                chatScroll.Height = Height;
                return;
            }

            chatScroll.Width = config.c_size_x.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? Width
                  : int.Parse(config.c_size_x);

            chatScroll.Height = config.c_size_y.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? Height
                  : int.Parse(config.c_size_y);

            chat.VerticalAlignment = VerticalAlignment.Stretch;
            chat.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void SetWindowLocation(Config config)
        {
            double screenTop = SystemParameters.WorkArea.Top;
            Top = screenTop;

            switch (config.w_pos_locate)
            {
                case "Left":
                    Left = SystemParameters.WorkArea.Left;
                    break;

                case "Right":
                    Left = SystemParameters.WorkArea.Right - Width;
                    break;

                case "Center":
                    Left = (SystemParameters.WorkArea.Width - Width) / 2;
                    break;

                case "Manual":
                    Left = int.Parse(config.w_pos_x);
                    Top = screenTop + int.Parse(config.w_pos_y);
                    break;
            }
        }

        private void SetChatLocation(Config config)
        {
            double left = 0;
            double top = 0;

            if (config.chat_on_window)
            {
                Canvas.SetLeft(chatScroll, left);
                Canvas.SetTop(chatScroll, top);
                return;
            }

            switch (config.c_pos_locate)
            {
                case "Left":
                    left = 0;
                    break;

                case "Right":
                    left = Width - chatScroll.Width;
                    break;

                case "Center":
                    left = (Width - chatScroll.Width) / 2;
                    break;

                case "Manual":
                    left = int.Parse(config.c_pos_x);
                    top = int.Parse(config.c_pos_y);
                    break;
            }

            Canvas.SetLeft(chatScroll, left);
            Canvas.SetTop(chatScroll, top);
        }

        private void SetBackground(Config config)
        {
            background_container.Background = Brushes.Transparent;
            background_container.Children.Clear();

            Opacity = int.Parse(config.b_opacity) / 100.0;

            if (config.transparent)
                return;

            switch (config.mode)
            {
                case "Palette":
                    background_container.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.b_color));
                    break;
                case "Image":
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(config.b_image_path)),
                    };

                    switch (config.b_image_fill_mode)
                    {
                        case "Uniform":
                            image.Stretch = Stretch.Uniform;
                            break;

                        case "Fill":
                            image.Stretch = Stretch.Fill;
                            break;

                        case "UniformTo":
                            image.Stretch = Stretch.UniformToFill;
                            break;

                        case "Manual":
                            var container = new Grid
                            {
                                Width = double.Parse(config.b_size_x),
                                Height = double.Parse(config.b_size_y),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(double.Parse(config.b_pos_x),
                                                       double.Parse(config.b_pos_y), 0, 0),
                                ClipToBounds = true
                            };

                            container.Children.Add(image);
                            background_container.Children.Add(container);
                            return;
                    }

                    background_container.Children.Add(image);
                    break;
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "Twitch Chat Overlay",
                Icon = Resource1.chat,
                ContextMenu = ContextMenu()
            };
        }

        private new ContextMenu ContextMenu()
        {
            var menu = new ContextMenu();

            var setting = new MenuItem { Header = "Открыть настройки" };
            setting.Click += (s, e) =>
            {
                new Setting().Show();
                _notifyIcon!.Dispose();
                this.Close();
            };

            var exit = new MenuItem { Header = "Выйти" };
            exit.Click += (s, e) =>
            {
                _notifyIcon!.Dispose();
                Application.Current.Shutdown();
            };

            menu.Items.Add(setting);
            menu.Items.Add(new Separator());
            menu.Items.Add(exit);

            return menu;
        }

        private void ChatBuilder(MessageParser.Message msg)
        {
            int size = int.Parse(config!.m_size);

            var message = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Width = chatScroll.ActualWidth,
                Margin = new Thickness(3),
                FontSize = size,
                FontFamily = new FontFamily(config.m_font),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(config.m_color)!,

                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 0,
                    Opacity = 0.7
                }
            };

            if (!string.IsNullOrWhiteSpace(msg.Reply))
            {
                message.Inlines.Add(new Run($"{msg.Reply}\n")
                {
                    FontStyle = FontStyles.Italic
                });
            }

            foreach (var b in msg.Badge)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(b)),
                    Width = size,
                    Height = size,
                };
                message.Inlines.Add(new InlineUIContainer(image));
                message.Inlines.Add(new Run(" "));
            }

            message.Inlines.Add(new Run($"{msg.Nickname}: ")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(msg.Color.R, msg.Color.G, msg.Color.B)),
                FontWeight = FontWeights.Bold
            });

            message.Inlines.Add(new Run($"{msg.Mention}"));

            message.Inlines.Add(new Run($"{msg.Content}"));

            message.Inlines.Add(new Run($"{msg.Link}")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(85, 26, 139))
            });

            foreach (var e in msg.Emote)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(e)),
                    Width = size,
                    Height = size,
                };
                message.Inlines.Add(new InlineUIContainer(image));
                message.Inlines.Add(new Run(" "));
            }

            chat.Children.Add(message);
            chatScroll.ScrollToBottom();

            if (chat.Children.Count > 50)
                chat.Children.RemoveAt(0);
        }

        private async Task ChatHandler(CancellationToken cancellationToken = default)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Args = [
                    "--renderer-process-limit=2",
                    "--disable-gpu",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-default-apps",
                    "--disable-sync",
                    "--disable-translate",
                    "--disable-logging"]

            });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(_link);

            int message_count = 0;
            int message_limit = 100;
            while (!cancellationToken.IsCancellationRequested)
            {
                var messages = page.Locator("div.chat-line__message-container");
                int count = await messages.CountAsync();

                if (count > message_limit)
                {
                    int remove_count = message_count;
                    await page.EvaluateAsync($@"
                            const elements = document.querySelectorAll('div.chat-line__message-container');
                            for (let i = 0; i < {remove_count}; i++) {{
                                if (elements[i]) elements[i].remove();
                                }}
                            ");

                    count -= remove_count;
                    message_count = 0;
                }

                for (int i = message_count; i < count; i++)
                {
                    MessageParser.Message msg = await MessageParser.GetChatAttributes(messages.Nth(i));
                    ChatBuilder(msg);
                }

                message_count = count;
                await Task.Delay(500, cancellationToken);
            }
        }

    }
}
