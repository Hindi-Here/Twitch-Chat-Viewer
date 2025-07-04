using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitchChatView
{
    public class MessageParser
    {
        public struct Message
        {
            public List<string> Badge { get; set; }
            public string Nickname { get; set; }
            public (byte R, byte G, byte B) Color { get; set; }
            public string Mention { get; set; }
            public string Content { get; set; }
            public List<string> Emote { get; set; }
            public string Reply { get; set; }
            public string Link { get; set; }
        }

        public static async Task<Message> GetChatAttributes(ILocator container)
        {
            Message msg = new()
            {
                Reply = await ReplySubstring(container),

                Badge = await ImageUrlList(
                    await container.Locator("img.chat-badge").AllAsync()
                ),

                Nickname = await container.Locator("span.chat-author__display-name").InnerTextAsync(),
                Color = await TextColor(container, "span.chat-author__display-name"),

                Mention = (await StringBuilderContent(
                    await container.Locator("span.mention-fragment").AllAsync()
                )).ToString(),

                Content = (await StringBuilderContent(
                    await container.Locator("span.text-fragment").AllAsync()
                )).ToString().Trim(),

                Emote = await ImageUrlList(
                     await container.Locator("img.chat-image.chat-line__message--emote").AllAsync()
                ),

                Link = (await StringBuilderAttribute(
                    await container.Locator("a.ScCoreLink-sc-16kq0mq-0.jUiaVy.link-fragment.tw-link").AllAsync(), "href"
                )).ToString()

            };

            return msg;
        }

        private static async Task<List<string>> ImageUrlList(IReadOnlyList<ILocator> list)
        {
            List<string>? imageURL = [];
            foreach (var L in list)
            {
                var src = await L.GetAttributeAsync("src");
                if (!string.IsNullOrEmpty(src))
                {
                    imageURL.Add(src);
                }
            }
            return imageURL;
        }

        private static async Task<StringBuilder> StringBuilderContent(IReadOnlyList<ILocator> list)
        {
            StringBuilder content = new();
            foreach (var L in list)
            {
                var text = await L.InnerTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    content.Append($"{text}");
                }
            }
            return content;
        }

        private static async Task<StringBuilder> StringBuilderAttribute(IReadOnlyList<ILocator> list, string attribute)
        {
            StringBuilder content = new();
            foreach (var L in list)
            {
                var text = await L.GetAttributeAsync(attribute);
                if (!string.IsNullOrEmpty(text))
                {
                    content.Append($"{text}");
                }
            }
            return content;
        }

        private static async Task<string> ReplySubstring(ILocator container)
        {
            int max_length = 35;
            string text = (await StringBuilderContent(
                await container.Locator("p.CoreText-sc-1txzju1-0.iWlGez").AllAsync()
                )).ToString();

            if (text.Length > max_length)
                text = string.Concat(text.AsSpan(0, max_length - 3), "...");

            return text;
        }

        private static async Task<(byte R, byte G, byte B)> TextColor(ILocator container, string selector)
        {
            string? style = await container.Locator(selector).GetAttributeAsync("style");
            Match match = Regex.Match(style!, @"rgb\((\d+),\s*(\d+),\s*(\d+)\)");

            return (byte.Parse(match.Groups[1].Value),
                    byte.Parse(match.Groups[2].Value),
                    byte.Parse(match.Groups[3].Value));
        }

    }
}
