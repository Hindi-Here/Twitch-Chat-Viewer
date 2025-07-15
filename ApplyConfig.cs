using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WpfAnimatedGif;
using System.IO;

namespace TwitchChatView
{
    internal class ApplyConfig(Chat chat, Config config)
    {
        private readonly Chat _chat = chat;
        private readonly Config? _config = config;

        public void Apply()
        {
            DefaultWindowSetting();

            SetWindowSize();
            SetWindowLocation();
            SetBackground();

            SetChatSize();
            SetChatLocation();
        }

        private void DefaultWindowSetting()
        {
            _chat.AllowsTransparency = true;
            _chat.WindowStyle = WindowStyle.None;
            _chat.ShowInTaskbar = false;
            _chat.Background = Brushes.Transparent;

            Win32.TopmostBehavior(_chat);
        }

        private void SetWindowSize()
        {
            _chat.Width = _config!.w_size_x.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? SystemParameters.PrimaryScreenWidth - 1
            : int.Parse(_config.w_size_x);

            _chat.Height = _config.w_size_y.Equals("auto", StringComparison.OrdinalIgnoreCase)
                   ? SystemParameters.PrimaryScreenHeight
                   : int.Parse(_config.w_size_y);
        }

        private void SetChatSize()
        {
            if (_config!.chat_on_window)
            {
                _chat.chat_scroll.Width = _chat.Width;
                _chat.chat_scroll.Height = _chat.Height;
                return;
            }

            _chat.chat_scroll.Width = _config.c_size_x.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? _chat.Width
                  : int.Parse(_config.c_size_x);

            _chat.chat_scroll.Height = _config.c_size_y.Equals("auto", StringComparison.OrdinalIgnoreCase)
                  ? _chat.Height
                  : int.Parse(_config.c_size_y);

            _chat.chat_zone.VerticalAlignment = VerticalAlignment.Stretch;
            _chat.chat_zone.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void SetWindowLocation()
        {
            double screenTop = SystemParameters.WorkArea.Top;
            _chat.Top = screenTop;

            switch (_config!.w_pos_locate)
            {
                case "Left":
                    _chat.Left = SystemParameters.WorkArea.Left;
                    break;

                case "Right":
                    _chat.Left = SystemParameters.WorkArea.Right - _chat.Width;
                    break;

                case "Center":
                    _chat.Left = (SystemParameters.WorkArea.Width - _chat.Width) / 2;
                    break;

                case "Manual":
                    _chat.Left = int.Parse(_config.w_pos_x);
                    _chat.Top = screenTop + int.Parse(_config.w_pos_y);
                    break;
            }
        }

        private void SetChatLocation()
        {
            double left = 0;
            double top = 0;

            if (_config!.chat_on_window)
            {
                Canvas.SetLeft(_chat.chat_scroll, left);
                Canvas.SetTop(_chat.chat_scroll, top);
                return;
            }

            switch (_config.c_pos_locate)
            {
                case "Left":
                    left = 0;
                    break;

                case "Right":
                    left = _chat.Width - _chat.chat_scroll.Width;
                    break;

                case "Center":
                    left = (_chat.Width - _chat.chat_scroll.Width) / 2;
                    break;

                case "Manual":
                    left = int.Parse(_config.c_pos_x);
                    top = int.Parse(_config.c_pos_y);
                    break;
            }

            Canvas.SetLeft(_chat.chat_scroll, left);
            Canvas.SetTop(_chat.chat_scroll, top);
        }

        private void SetBackground()
        {
            _chat.background_container.Background = Brushes.Transparent;
            _chat.background_container.Children.Clear();

            _chat.Opacity = int.Parse(_config!.b_opacity) / 100.0;

            if (_config.transparent)
                return;

            switch (_config.mode)
            {
                case "Palette":
                    _chat.background_container.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_config.b_color));
                    break;
                case "Image":
                    var image = new Image();
                    SetImageSource(image, new Uri(_config.b_image_path));

                    switch (_config.b_image_fill_mode)
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
                                Width = double.Parse(_config.b_size_x),
                                Height = double.Parse(_config.b_size_y),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(double.Parse(_config.b_pos_x),
                                                       double.Parse(_config.b_pos_y), 0, 0),
                                ClipToBounds = true
                            };

                            container.Children.Add(image);
                            _chat.background_container.Children.Add(container);
                            return;
                    }

                    _chat.background_container.Children.Add(image);
                    break;
            }
        }

        private void SetImageSource(Image image, Uri uri)
        {
            if (Path.GetExtension(_config!.b_image_path).Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                var gif = new BitmapImage();
                gif.BeginInit();
                gif.UriSource = uri;
                gif.EndInit();

                ImageBehavior.SetAnimatedSource(image, gif);
                ImageBehavior.SetRepeatBehavior(image, System.Windows.Media.Animation.RepeatBehavior.Forever);
            }
            else
            {
                image.Source = new BitmapImage(uri);
            }
        }

    }
}