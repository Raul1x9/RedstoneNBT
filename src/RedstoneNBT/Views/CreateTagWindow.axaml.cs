using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Substrate.Nbt;

namespace RedstoneNBT.Views;

public partial class CreateTagWindow : Window
{
    private readonly TagType _tagType;
    private readonly bool _hasName;
    private readonly HashSet<string> _existingNames;

    public TagNode? ResultTag { get; private set; }
    public string ResultName { get; private set; } = string.Empty;

    public CreateTagWindow()
    {
        InitializeComponent();
        _tagType = TagType.TAG_BYTE;
        _hasName = true;
        _existingNames = new HashSet<string>(StringComparer.Ordinal);
    }

    public CreateTagWindow(TagType type, bool hasName, IEnumerable<string>? existingNames = null)
    {
        InitializeComponent();
        _tagType = type;
        _hasName = hasName;
        _existingNames = existingNames != null
            ? new HashSet<string>(existingNames, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        TitleText.Text = $"Add {type}";
        SubtitleText.Text = $"Create new {type} node";

        NamePanel.IsVisible = _hasName;
        bool isScalar = type switch
        {
            TagType.TAG_BYTE or TagType.TAG_SHORT or TagType.TAG_INT or
            TagType.TAG_LONG or TagType.TAG_FLOAT or TagType.TAG_DOUBLE or
            TagType.TAG_STRING => true,
            _ => false
        };

        ValuePanel.IsVisible = isScalar;
        if (isScalar)
        {
            ValueTextBox.Text = type switch
            {
                TagType.TAG_FLOAT or TagType.TAG_DOUBLE => "0.0",
                TagType.TAG_STRING => "",
                _ => "0"
            };
        }

        if (type == TagType.TAG_LIST)
        {
            ListTypePanel.IsVisible = true;
            ListTypeComboBox.ItemsSource = new[]
            {
                TagType.TAG_COMPOUND,
                TagType.TAG_STRING,
                TagType.TAG_INT,
                TagType.TAG_DOUBLE,
                TagType.TAG_FLOAT,
                TagType.TAG_BYTE,
                TagType.TAG_SHORT,
                TagType.TAG_LONG,
                TagType.TAG_BYTE_ARRAY,
                TagType.TAG_INT_ARRAY,
                TagType.TAG_LONG_ARRAY,
                TagType.TAG_LIST
            };
            ListTypeComboBox.SelectedItem = TagType.TAG_COMPOUND;
        }

        Validate();
    }

    private void Input_Changed(object? sender, TextChangedEventArgs e)
    {
        Validate();
    }

    private bool Validate()
    {
        if (OkButton == null || ErrorText == null) return false;

        bool valid = true;
        string error = string.Empty;

        if (_hasName)
        {
            string name = NameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                valid = false;
                error = "Tag name cannot be empty.";
            }
            else if (_existingNames.Contains(name))
            {
                valid = false;
                error = $"A tag named '{name}' already exists.";
            }
        }

        if (valid && ValuePanel.IsVisible)
        {
            string val = ValueTextBox.Text ?? string.Empty;
            switch (_tagType)
            {
                case TagType.TAG_BYTE:
                    if (!sbyte.TryParse(val, out _) && !byte.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a valid byte (-128 to 255).";
                    }
                    break;
                case TagType.TAG_SHORT:
                    if (!short.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a 16-bit integer.";
                    }
                    break;
                case TagType.TAG_INT:
                    if (!int.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a 32-bit integer.";
                    }
                    break;
                case TagType.TAG_LONG:
                    if (!long.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a 64-bit integer.";
                    }
                    break;
                case TagType.TAG_FLOAT:
                    if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                        !float.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a valid float.";
                    }
                    break;
                case TagType.TAG_DOUBLE:
                    if (!double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                        !double.TryParse(val, out _))
                    {
                        valid = false;
                        error = "Must be a valid double.";
                    }
                    break;
            }
        }

        ErrorText.Text = error;
        ErrorText.IsVisible = !valid;
        OkButton.IsEnabled = valid;

        return valid;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        ResultName = _hasName ? (NameTextBox.Text?.Trim() ?? string.Empty) : string.Empty;
        string val = ValueTextBox.Text ?? string.Empty;

        ResultTag = _tagType switch
        {
            TagType.TAG_BYTE => sbyte.TryParse(val, out sbyte sb) ? new TagNodeByte((byte)sb) : new TagNodeByte(byte.TryParse(val, out byte b) ? b : (byte)0),
            TagType.TAG_SHORT => new TagNodeShort(short.TryParse(val, out short s) ? s : (short)0),
            TagType.TAG_INT => new TagNodeInt(int.TryParse(val, out int i) ? i : 0),
            TagType.TAG_LONG => new TagNodeLong(long.TryParse(val, out long l) ? l : 0L),
            TagType.TAG_FLOAT => new TagNodeFloat(float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0.0f),
            TagType.TAG_DOUBLE => new TagNodeDouble(double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0),
            TagType.TAG_STRING => new TagNodeString(val),
            TagType.TAG_BYTE_ARRAY => new TagNodeByteArray(new byte[0]),
            TagType.TAG_INT_ARRAY => new TagNodeIntArray(new int[0]),
            TagType.TAG_LONG_ARRAY => new TagNodeLongArray(new long[0]),
            TagType.TAG_LIST => new TagNodeList((TagType)(ListTypeComboBox.SelectedItem ?? TagType.TAG_COMPOUND)),
            TagType.TAG_COMPOUND => new TagNodeCompound(),
            _ => new TagNodeByte(0)
        };

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
