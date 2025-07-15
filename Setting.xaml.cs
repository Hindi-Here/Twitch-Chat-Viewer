using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TwitchChatView
{
    public partial class Setting : Window
    {
        private readonly ManagerConfig _managerConfig;

        public Setting()
        {
            InitializeComponent();
            Icon = Imaging.CreateBitmapSourceFromHIcon(
                   Resource1.chat.Handle,
                   Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());

            _managerConfig = new ManagerConfig(GetElementAndPropertyMap());
            _managerConfig.LoadConfig();

            StartSettingState();
        }
        private void LaunchChatForm(object sender, RoutedEventArgs e)
        {
            _managerConfig.BuildConfig();
            new Chat(link.Text).Show();
            this.Close();
        }

        private Dictionary<UIElement, string> GetElementAndPropertyMap()
        {
            return new Dictionary<UIElement, string>
            {
                { w_pos_radio_group, "w_pos_locate" },
                { w_pos_x, "w_pos_x" },
                { w_pos_y, "w_pos_y" },
                { w_size_x, "w_size_x" },
                { w_size_y, "w_size_y" },

                { c_chat_on_window, "chat_on_window" },
                { c_pos_radio_group, "c_pos_locate" },
                { c_pos_x, "c_pos_x" },
                { c_pos_y, "c_pos_y" },
                { c_size_x, "c_size_x" },
                { c_size_y, "c_size_y" },

                { m_size, "m_size" },
                { m_font, "m_font" },
                { m_color, "m_color" },
                { m_color_preview, "m_color" },

                { b_transparent, "transparent" },
                { b_radio_group, "mode" },
                { b_color, "b_color" },
                { b_color_preview, "b_color" },
                { b_image_path, "b_image_path" },
                { b_fill_radio_group, "b_image_fill_mode" },
                { b_pos_image_x, "b_pos_x" },
                { b_pos_image_y, "b_pos_y" },
                { b_size_image_x, "b_size_x" },
                { b_size_image_y, "b_size_y" },
                { b_opacity, "b_opacity" }
            };
        }

        private void StartSettingState()
        { 
            UpdateWindowState();
            UpdateChatState();
            UpdateBackgroundState();
        }

        private static void SetGroupState(UIElement group, bool isEnabled)
        {
            group.IsEnabled = isEnabled;
            group.Opacity = isEnabled ? 1.0 : 0.5;
        }

        private void UpdateWindowState()
        {
            if (w_pos_manual_group == null)
                return;

            bool isManual = w_pos_manual.IsChecked == true;
            SetGroupState(w_pos_manual_group, isManual);
        }

        private void UpdateWindowState(object sender, RoutedEventArgs e)
            => UpdateWindowState();

        private void UpdateChatState()
        {
            if (c_pos_manual_group == null)
                return;

            bool isChatOnWindow = c_chat_on_window.IsChecked == true;
            bool isManual = c_pos_manual.IsChecked == true;

            SetGroupState(c_pos_radio_group, !isChatOnWindow);
            SetGroupState(c_size_group, !isChatOnWindow);
            SetGroupState(c_pos_manual_group, !isChatOnWindow && isManual);
        }

        private void UpdateChatState(object sender, RoutedEventArgs e)
            => UpdateChatState();

        private void UpdateBackgroundState()
        {
            if (b_radio_group == null || b_image == null || b_fill_manual == null)
                return;

            bool isTransparent = b_transparent.IsChecked == true;
            bool isPalette = b_palette.IsChecked == true;
            bool isImage = b_image.IsChecked == true;
            bool isNullImagePath = string.IsNullOrEmpty(b_image_path.Text);
            bool isManualFill = b_fill_manual.IsChecked == true;

            SetGroupState(b_radio_group, !isTransparent);
            SetGroupState(b_color_group, !isTransparent && isPalette);
            SetGroupState(b_image_group, !isTransparent && isImage);
            SetGroupState(b_fill_radio_group,!isTransparent && isImage && !isNullImagePath);
            SetGroupState(b_pos_image_group, !isTransparent && isImage && !isNullImagePath && isManualFill);
            SetGroupState(b_size_image_group, !isTransparent && isImage && !isNullImagePath && isManualFill);
            SetGroupState(b_opacity_group, !isTransparent);
        }

        private void UpdateBackgroundState(object sender, RoutedEventArgs? e)
            => UpdateBackgroundState();

        private void UpdateBackgroundState(object sender, TextChangedEventArgs e)
            => UpdateBackgroundState();

        private void UpdateLaunchState(object sender, TextChangedEventArgs e)
           => launch.IsEnabled = !string.IsNullOrEmpty(link.Text);

        private void TextBoxValueValidator(TextBox textBox)
        {
            string name = textBox.Name;
            bool isInt = int.TryParse(textBox.Text, out int value);

            switch (name)
            {
                case string when name is "w_pos_x" or "w_pos_y" or
                                         "c_pos_x" or "c_pos_y" or
                                         "b_pos_image_x" or "b_pos_image_y":

                    if (!isInt)
                        textBox.Text = _managerConfig.GetConfigValue(name);

                    break;

                case string when name is "w_size_x" or "w_size_y" or
                                         "c_size_x" or "c_size_y" or
                                         "b_size_image_x" or "b_size_image_y":

                    if ((!isInt || value < 0) && !textBox.Text.Equals("auto", StringComparison.OrdinalIgnoreCase))
                        textBox.Text = _managerConfig.GetConfigValue(name);

                    break;

                case string when name is "m_size" or "b_opacity":
                    if (!isInt || value > 100 || value < 0)
                        textBox.Text = _managerConfig.GetConfigValue(name);

                    break;

                case string when name is "m_color" or "b_color":
                    string color = Regex.IsMatch(textBox.Text, @"^#(?:[0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$", RegexOptions.IgnoreCase)
                                 ? textBox.Text
                                 : _managerConfig.GetConfigValue(name);

                    textBox.Text = color;
                    ManagerConfig.SelectedColorShow((Label)textBox.Tag, color);

                    break;
            }
        }

        private void TextBoxValueValidator(object sender, RoutedEventArgs e)
            => TextBoxValueValidator((TextBox)sender);

        private void ChooseBackgroundColor(object sender, RoutedEventArgs e)
        {
            Label label = (Label)sender;
            SolidColorBrush solid = (SolidColorBrush)label.Background;

            color_picker_popup.PlacementTarget = label;
            color_picker_popup.IsOpen = true;

            color_picker.Color = solid.Color;
            alpha_slider.Value = solid.Color.A / 255.0 * 100;
        }

        private void UpdateColorValue()
        {
            if (color_picker_popup.PlacementTarget is Label label && label.Tag is TextBox textBox)
            {
                byte alpha = (byte)(alpha_slider.Value / 100 * 255);
                Color color = color_picker.Color;
                Color colorA = Color.FromArgb(alpha, color.R, color.G, color.B);

                label.Background = new SolidColorBrush(colorA);
                textBox.Text = $"#{colorA.A:X2}{colorA.R:X2}{colorA.G:X2}{colorA.B:X2}";
            }
        }

        private void UpdateColorValue(object sender, MouseEventArgs e)
            => UpdateColorValue();

        private void UpdateColorValue(object sender, RoutedPropertyChangedEventArgs<double> e)
            => UpdateColorValue();

        private void ChooseBackgroundImage(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose Image",
                Filter = "Images (*.png, *.jpg, *.jpeg, *.gif, *.bmp, *.tiff)|*.png; *.jpg; *.jpeg; *.gif; *.bmp; *.tiff",
            };

            if (dialog.ShowDialog() == true)
            {
                string selected = dialog.FileName;
                b_image_path.Text = selected;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selected);
                bitmap.EndInit();

                b_size_image_x.Text = bitmap.PixelWidth.ToString();
                b_size_image_y.Text = bitmap.PixelHeight.ToString();

                bitmap.Freeze();
            }
        }

        private void MinimizeForm(object sender, MouseButtonEventArgs e)
           => this.WindowState = WindowState.Minimized;

        private void CloseForm(object sender, MouseButtonEventArgs e)
           => this.Close();

        private void DragForm(object sender, MouseButtonEventArgs e)
            => this.DragMove();

    }
}