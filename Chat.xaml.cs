using Microsoft.Playwright;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Media.Effects;
using System.Xml.Linq;

namespace TwitchChatView
{
    public partial class Chat : Window
    {
        private readonly string _link;

        private TaskbarIcon? _notifyIcon;
        private readonly ManagerConfig _managerConfig = new();

        private IBrowser? _browser;
        private IPlaywright? _playwright;
        private IPage? _page;
        private CancellationTokenSource? _cts;

        public Chat(string link)
        {
            InitializeComponent();

            SetupConfig();
            SetupTrayIcon();

            _link = link;
        }

        private void SetupConfig()
        {
            _managerConfig.ApplyConfigToChat(this);
        }

        private async void ChatLoaded(object sender, RoutedEventArgs e)
            => await ChatHandler();

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

            var setting = new MenuItem { Header = "Open Setting" };
            setting.Click += async (s, e) =>
            {
                await BackToSetting();
            };

            var exit = new MenuItem { Header = "Exit" };
            exit.Click += async (s, e) =>
            {
                await ResourceDispose();
                Application.Current.Shutdown();
            };

            menu.Items.Add(setting);
            menu.Items.Add(new Separator());
            menu.Items.Add(exit);

            return menu;
        }

        private async Task ResourceDispose()
        {
            await Task.Run(() => _cts?.Cancel());

            _notifyIcon?.Dispose();

            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;
        }

        private async Task BackToSetting()
        {
            await ResourceDispose();

            if (Application.Current.Windows.OfType<Setting>().FirstOrDefault() == null)
                new Setting().Show();

            this.Close();
        }

        private void ChatBuilder(MessageParser.Message msg)
        {
            int size = int.Parse(_managerConfig.GetConfigValue("m_size"));

            var message = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Width = chat_scroll.ActualWidth,
                Margin = new Thickness(3),
                FontSize = size,
                FontFamily = new FontFamily(_managerConfig.GetConfigValue("m_font")),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(_managerConfig.GetConfigValue("m_color"))!,
                Padding = new Thickness(0,0,5,0),

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

            chat_zone.Children.Add(message);
            chat_scroll.ScrollToBottom();

            if (chat_zone.Children.Count > 50)
                chat_zone.Children.RemoveAt(0);
        }

        private async Task ChatHandler()
        {
            _cts = new CancellationTokenSource()!;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Args =
                [
                "--renderer-process-limit=2",
                "--disable-gpu",
                "--disable-extensions",
                "--disable-background-networking",
                "--disable-default-apps",
                "--disable-sync",
                "--disable-translate",
                "--disable-logging"
                ]
            });

            try
            {
                _page = await _browser.NewPageAsync();
                await _page.GotoAsync(_link);

                await Task.Delay(1000);
                var chatElement = await _page.Locator("div.Layout-sc-1xcs6mc-0.gyMdFQ.stream-chat").CountAsync();
                var offline = await _page.Locator("text=Не в сети").CountAsync();
                if (chatElement == 0 || offline > 0)
                {
                    await BackToSetting();
                    return;
                }
            }
            catch (Exception) {
                await BackToSetting();
                return;
            }

            int messageCount = 0;
            int messageLimit = 100;
            while (!_cts.IsCancellationRequested)
            {
                var messages = _page.Locator("div.chat-line__message-container");
                int count = await messages.CountAsync();

                if (count > messageLimit)
                {
                    int removeCount = messageCount;
                    await _page.EvaluateAsync($@"
                            const elements = document.querySelectorAll('div.chat-line__message-container');
                            for (let i = 0; i < {removeCount}; i++) {{
                                if (elements[i]) elements[i].remove();
                                }}
                            ");

                    count -= removeCount;
                    messageCount = 0;
                }

                for (int i = messageCount; i < count; i++)
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    try
                    {
                        MessageParser.Message msg = await MessageParser.GetChatAttributes(messages.Nth(i));
                        ChatBuilder(msg);
                    }
                    catch(Exception)
                    {
                        await BackToSetting();
                        return;
                    }
                }

                messageCount = count;
                await Task.Delay(50);
                
            }
            await ResourceDispose();
        }

    }
}
