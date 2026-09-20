using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Substrate.Nbt;

namespace NBTReborn.Views;

public partial class EditByteArrayWindow : Window
{
    private readonly TagNode _tag;

    public EditByteArrayWindow()
    {
        InitializeComponent();
        _tag = new TagNodeByteArray(new byte[0]);
    }

    public EditByteArrayWindow(TagNode tag, string? tagName = null)
    {
        InitializeComponent();
        _tag = tag;

        string name = string.IsNullOrEmpty(tagName) ? "Array" : tagName;
        TitleText.Text = $"Edit {tag.GetTagType()}: {name}";

        if (tag is TagNodeByteArray ba)
        {
            SubtitleText.Text = $"Byte Array ({ba.Length} bytes). Values from -128 to 255 or 0x00 hex.";
            ArrayDataTextBox.Text = string.Join(", ", ba.Data);
        }
        else if (tag is TagNodeIntArray ia)
        {
            SubtitleText.Text = $"Int Array ({ia.Length} integers). Values from -2147483648 to 2147483647.";
            ArrayDataTextBox.Text = string.Join(", ", ia.Data);
        }
        else if (tag is TagNodeLongArray la)
        {
            SubtitleText.Text = $"Long Array ({la.Length} long integers).";
            ArrayDataTextBox.Text = string.Join(", ", la.Data);
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        string text = ArrayDataTextBox.Text ?? string.Empty;
        var parts = text.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (_tag is TagNodeByteArray ba)
        {
            var bytes = new List<byte>();
            foreach (var part in parts)
            {
                if (part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                    byte.TryParse(part.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexB))
                {
                    bytes.Add(hexB);
                }
                else if (sbyte.TryParse(part, out sbyte sb))
                {
                    bytes.Add((byte)sb);
                }
                else if (byte.TryParse(part, out byte ub))
                {
                    bytes.Add(ub);
                }
            }
            ba.Data = bytes.ToArray();
        }
        else if (_tag is TagNodeIntArray ia)
        {
            var ints = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int val))
                {
                    ints.Add(val);
                }
            }
            ia.Data = ints.ToArray();
        }
        else if (_tag is TagNodeLongArray la)
        {
            var longs = new List<long>();
            foreach (var part in parts)
            {
                if (long.TryParse(part, out long val))
                {
                    longs.Add(val);
                }
            }
            la.Data = longs.ToArray();
        }

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
