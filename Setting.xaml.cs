using System.IO;
using System.Text.Json;
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
        readonly string config_path = Path.Combine(@"..\..\..\config.json");
        public Setting()
        {
            InitializeComponent();
            Icon = Imaging.CreateBitmapSourceFromHIcon(
                   Resource1.chat.Handle,
                   Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());

            ConfigLoader();
            StartSettingState();
        }
        private void LaunchChatForm(object sender, RoutedEventArgs e)
        {
            ConfigBuilder();
            new Chat(link.Text).Show();
            this.Close();
        }

        private void ConfigLoader()
        {
            string path = config_path;
            if (!File.Exists(path))
                ConfigBuilder();

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<Config>(json);

            SetRadioButton(w_pos_radio_group, config!.w_pos_locate);
            w_pos_x.Text = config.w_pos_x;
            w_pos_y.Text = config.w_pos_y;
            w_size_x.Text = config.w_size_x;
            w_size_y.Text = config.w_size_y;

            c_chat_on_window.IsChecked = config.chat_on_window;
            SetRadioButton(c_pos_radio_group, config.c_pos_locate);
            c_pos_x.Text = config.c_pos_x;
            c_pos_y.Text = config.c_pos_y;
            c_size_x.Text = config.c_size_x;
            c_size_y.Text = config.c_size_y;

            m_size.Text = config.m_size;
            m_font.Text = config.m_font;
            ColorShow(m_color_preview, config.m_color);
            m_color.Text = config.m_color;

            b_transparent.IsChecked = config.transparent;
            SetRadioButton(b_radio_group, config.mode);
            ColorShow(b_color_preview, config.b_color);
            b_color.Text = config.b_color;
            b_image_path.Text = config.b_image_path;
            SetRadioButton(b_fill_radio_group, config.b_image_fill_mode);
            b_pos_image_x.Text = config.b_pos_x;
            b_pos_image_y.Text = config.b_pos_y;
            b_size_image_x.Text = config.b_size_x;
            b_size_image_y.Text = config.b_size_y;
            b_opacity.Text = config.b_opacity;
        }

        private void ConfigBuilder()
        {
            var config = new Config
            {
                w_pos_locate = GetRadioButtonContent(w_pos_radio_group),
                w_pos_x = w_pos_x.Text,
                w_pos_y = w_pos_y.Text,
                w_size_x = w_size_x.Text,
                w_size_y = w_size_y.Text,

                chat_on_window = c_chat_on_window.IsChecked == true,
                c_pos_locate = GetRadioButtonContent(c_pos_radio_group),
                c_pos_x = c_pos_x.Text,
                c_pos_y = c_pos_y.Text,
                c_size_x = c_size_x.Text,
                c_size_y = c_size_y.Text,

                m_size = m_size.Text,
                m_font = m_font.Text,
                m_color = m_color.Text,

                transparent = b_transparent.IsChecked == true,
                mode = GetRadioButtonContent(b_radio_group),
                b_color = b_color.Text,
                b_image_path = b_image_path.Text,
                b_image_fill_mode = GetRadioButtonContent(b_fill_radio_group),
                b_pos_x = b_pos_image_x.Text,
                b_pos_y = b_pos_image_y.Text,
                b_size_x = b_size_image_x.Text,
                b_size_y = b_size_image_y.Text,
                b_opacity = b_opacity.Text
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });

            File.WriteAllText(config_path, json);
        }

        private static string GetRadioButtonContent(StackPanel radioGroup)
        {
            foreach (var radio in radioGroup.Children.OfType<RadioButton>())
                if (radio.IsChecked == true)
                    return radio.Content.ToString()!;

            return string.Empty;
        }

        private static void SetRadioButton(StackPanel radioGroup, string value)
        {
            foreach (var radio in radioGroup.Children.OfType<RadioButton>())
                if (radio.Content.ToString() == value)
                {
                    radio.IsChecked = true;
                    break;
                }
        }

        private void StartSettingState()
        { 
            UpdateWindowState();
            UpdateChatState();
            UpdateBackgroundState();
        }

        private void UpdateWindowState()
        {
            if (w_pos_manual_group == null)
                return;

            bool isManual = w_pos_manual.IsChecked == true;
            w_pos_manual_group.IsEnabled = isManual;
            w_pos_manual_group.Opacity = isManual ? 1.0 : 0.5;
        }

        private void UpdateWindowState(object sender, RoutedEventArgs e)
            => UpdateWindowState();

        private void UpdateChatState()
        {
            if (c_pos_manual_group == null)
                return;

            bool isChatOnWindow = c_chat_on_window.IsChecked == true;
            bool isManual = c_pos_manual.IsChecked == true;

            bool enabled = !isChatOnWindow;
            double opacity = isChatOnWindow ? 0.5 : 1.0;

            c_pos_radio_group.IsEnabled = enabled;
            c_pos_radio_group.Opacity = opacity;

            c_size_group.IsEnabled = enabled;
            c_size_group.Opacity = opacity;

            c_pos_manual_group.IsEnabled = enabled && isManual;
            c_pos_manual_group.Opacity = isManual ? opacity : 0.5;
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

            double opacity = isTransparent ? 0.5 : 1.0;

            b_radio_group.IsEnabled = !isTransparent;
            b_radio_group.Opacity = opacity;

            b_color_group.IsEnabled = !isTransparent && isPalette;
            b_color_group.Opacity = isPalette ? opacity : 0.5;

            b_image_group.IsEnabled = !isTransparent && isImage;
            b_image_group.Opacity = isImage ? opacity : 0.5;

            b_fill_radio_group.IsEnabled = !isTransparent && isImage && !isNullImagePath;
            b_fill_radio_group.Opacity = (isImage && !isNullImagePath) ? opacity : 0.5;

            b_pos_image_group.IsEnabled = !isTransparent && isImage && !isNullImagePath && isManualFill;
            b_pos_image_group.Opacity = (isImage && !isNullImagePath && isManualFill) ? opacity : 0.5;

            b_size_image_group.IsEnabled = !isTransparent && isImage && !isNullImagePath && isManualFill;
            b_size_image_group.Opacity = (isImage && !isNullImagePath && isManualFill) ? opacity : 0.5;

            b_opacity_group.IsEnabled = !isTransparent;
            b_opacity_group.Opacity = opacity;
        }

        private void UpdateBackgroundState(object sender, RoutedEventArgs? e)
            => UpdateBackgroundState();

        private void UpdateBackgroundState(object sender, TextChangedEventArgs e)
            => UpdateBackgroundState();

        private void UpdateLaunchState(object sender, TextChangedEventArgs e)
           => launch.IsEnabled = !string.IsNullOrEmpty(link.Text);

        private static void TextBoxValueValidator(TextBox textBox)
        {
            string name = textBox.Name;
            switch (name)
            {
                case string when name is "w_pos_x" or "w_pos_y" or
                                         "c_pos_x" or "c_pos_y" or
                                         "b_pos_image_x" or "b_pos_image_y":

                    if (!int.TryParse(textBox.Text, out _))
                        textBox.Text = "0";

                    break;

                case string when name is "w_size_x" or "w_size_y" or
                                         "c_size_x" or "c_size_y" or
                                         "b_size_image_x" or "b_size_image_y":

                    if (!int.TryParse(textBox.Text, out _) && !textBox.Text.Equals("auto", StringComparison.OrdinalIgnoreCase))
                        textBox.Text = "400";

                    break;

                case "m_size":
                    if (!int.TryParse(textBox.Text, out int num) || num > 100 || num < 0)
                        textBox.Text = "12";

                    break;

                case string when name is "m_color" or "b_color":
                    string color = Regex.IsMatch(textBox.Text, @"^#(?:[0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$", RegexOptions.IgnoreCase)
                                 ? textBox.Text
                                 : "#FFFFFF";

                    textBox.Text = color;
                    ColorShow((Label)textBox.Tag, color);

                    break;

                case "b_opacity":
                    if (!int.TryParse(textBox.Text, out int opacity) || opacity > 100 || opacity < 0)
                        textBox.Text = "0";

                    break;
            }
        }

        private static void ColorShow(Label label, string color)
        {
            label.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
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
                Color color_alpha = Color.FromArgb(alpha, color.R, color.G, color.B);

                label.Background = new SolidColorBrush(color_alpha);
                textBox.Text = $"#{color_alpha.A:X2}{color_alpha.R:X2}{color_alpha.G:X2}{color_alpha.B:X2}";
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
                string selected_path = dialog.FileName;

                b_image_path.Text = selected_path;

                using var img = System.Drawing.Image.FromFile(selected_path);
                b_size_image_x.Text = img.Width.ToString();
                b_size_image_y.Text = img.Height.ToString();
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