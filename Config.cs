using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace TwitchChatView
{
    internal class Config
    {
        public required string w_pos_locate { get; set; }
        public required string w_pos_x { get; set; }
        public required string w_pos_y { get; set; }
        public required string w_size_x { get; set; }
        public required string w_size_y { get; set; }

        public bool chat_on_window { get; set; }
        public required string c_pos_locate { get; set; }
        public required string c_pos_x { get; set; }
        public required string c_pos_y { get; set; }
        public required string c_size_x { get; set; }
        public required string c_size_y { get; set; }

        public required string m_size { get; set; }
        public required string m_font { get; set; }
        public required string m_color { get; set; }

        public bool transparent { get; set; }
        public required string mode { get; set; }
        public required string b_color { get; set; }
        public required string b_image_path { get; set; }
        public required string b_image_fill_mode { get; set; }
        public required string b_pos_x { get; set; }
        public required string b_pos_y { get; set; }
        public required string b_size_x { get; set; }
        public required string b_size_y { get; set; }
        public required string b_opacity { get; set; }
    }
}
