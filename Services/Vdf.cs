using System;
using System.Collections.Generic;
using System.Text;

namespace SeaLauncher.Services
{
    public class VdfNode
    {
        public string Value { get; set; }
        public Dictionary<string, VdfNode> Children { get; } = new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase);

        public string Get(string key)
        {
            VdfNode node;
            if (Children.TryGetValue(key, out node))
            {
                return node.Value;
            }
            return null;
        }

        public VdfNode Child(string key)
        {
            VdfNode node;
            if (Children.TryGetValue(key, out node))
            {
                return node;
            }
            return null;
        }
    }

    public static class Vdf
    {
        private const int End = 0;
        private const int Str = 1;
        private const int Open = 2;
        private const int Close = 3;

        public static VdfNode Parse(string text)
        {
            var root = new VdfNode();
            int pos = 0;
            ReadChildren(text ?? "", ref pos, root, 0);
            return root;
        }

        private static void ReadChildren(string text, ref int pos, VdfNode node, int depth)
        {
            if (depth > 64)
            {
                return;
            }

            while (true)
            {
                string key;
                int kind = Next(text, ref pos, out key);
                if (kind == End || kind == Close)
                {
                    return;
                }
                if (kind != Str)
                {
                    continue;
                }

                string value;
                int kind2 = Next(text, ref pos, out value);
                if (kind2 == Open)
                {
                    var child = new VdfNode();
                    ReadChildren(text, ref pos, child, depth + 1);
                    node.Children[key] = child;
                }
                else if (kind2 == Str)
                {
                    node.Children[key] = new VdfNode { Value = value };
                }
                else
                {
                    return;
                }
            }
        }

        private static int Next(string text, ref int pos, out string value)
        {
            value = null;
            while (pos < text.Length)
            {
                char c = text[pos];
                if (c == '{')
                {
                    pos++;
                    return Open;
                }
                if (c == '}')
                {
                    pos++;
                    return Close;
                }
                if (c == '"')
                {
                    pos++;
                    var builder = new StringBuilder();
                    while (pos < text.Length)
                    {
                        char d = text[pos];
                        if (d == '\\' && pos + 1 < text.Length)
                        {
                            char escaped = text[pos + 1];
                            if (escaped == 'n')
                            {
                                builder.Append('\n');
                            }
                            else if (escaped == 't')
                            {
                                builder.Append('\t');
                            }
                            else
                            {
                                builder.Append(escaped);
                            }
                            pos += 2;
                            continue;
                        }
                        if (d == '"')
                        {
                            pos++;
                            break;
                        }
                        builder.Append(d);
                        pos++;
                    }
                    value = builder.ToString();
                    return Str;
                }
                pos++;
            }
            return End;
        }
    }
}
