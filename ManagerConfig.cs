using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TwitchChatView
{
    class ManagerConfig
    {
        readonly string _path = Path.Combine(@"..\..\..\config.json");
        private readonly Dictionary<UIElement, string> _map;
        private Config? _config;

        public ManagerConfig(Dictionary<UIElement, string> map)
        {
            _map = map;
            _config = ReadConfig();
        }
        public ManagerConfig()
        {
            _map = [];
            _config = ReadConfig();
        }

        public Config ReadConfig()
        {
            if (!File.Exists(_path))
            {
                _config = new Config();
                BuildConfig();
            }

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Config>(json)!;
        }

        private void SaveConfig()
        {
            string json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(_path, json);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public void LoadConfig()
        {
            foreach (var (element, property) in _map)
            {
                string value = GetConfigValue(property);

                switch (element)
                {
                    case TextBox textBox:
                        textBox.Text = value;
                        break;

                    case StackPanel radioGroup:
                        SetRadioButton(radioGroup, value);
                        break;

                    case CheckBox checkBox:
                        checkBox.IsChecked = bool.TryParse(value, out bool isChecked) && isChecked;
                        break;

                    case Label label:
                        SelectedColorShow(label, value);
                        break;
                }
            }
        }

        public void BuildConfig()
        {
            foreach (var (element, property) in _map)
            {
                var info = typeof(Config).GetProperty(property);

                switch (element)
                {
                    case TextBox textBox:
                        info?.SetValue(_config, textBox.Text);
                        break;

                    case StackPanel radioGroup:
                        info?.SetValue(_config, GetRadioButtonContent(radioGroup));
                        break;

                    case CheckBox checkBox:
                        info?.SetValue(_config, checkBox.IsChecked ?? false);
                        break;
                }
            }
            SaveConfig();
        }

        public void ApplyConfigToChat(Chat chat)
        {
            new ApplyConfig(chat, _config!).Apply();
        }

        public string GetConfigValue(string name)
        {
            return typeof(Config).GetProperty(name)?.GetValue(_config)?.ToString()!;
        }

        public static void SelectedColorShow(Label label, string color)
        {
            label.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private static string GetRadioButtonContent(StackPanel radioGroup)
        {
            return radioGroup.Children.OfType<RadioButton>()
                .FirstOrDefault(r => r.IsChecked == true)?.Content.ToString() ?? string.Empty;
        }

        private static void SetRadioButton(StackPanel radioGroup, string value)
        {
            var radio = radioGroup.Children.OfType<RadioButton>()
                .FirstOrDefault(r => r.Content.ToString() == value);

            if(radio != null)
                radio.IsChecked = true;
        }
    }
}
