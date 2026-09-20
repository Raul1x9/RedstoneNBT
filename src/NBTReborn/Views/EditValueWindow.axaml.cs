using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Substrate.Nbt;

namespace NBTReborn.Views;

public partial class EditValueWindow : Window
{
    private readonly TagNode _tag;
    private readonly TagType _tagType;

    public string ResultValue { get; private set; } = string.Empty;

    public EditValueWindow()
    {
        InitializeComponent();
        _tag = new TagNodeByte(0);
        _tagType = TagType.TAG_BYTE;
    }

    public EditValueWindow(TagNode tag, string? tagName = null)
    {
        InitializeComponent();
        _tag = tag;
        _tagType = tag.GetTagType();

        TagInfoText.Text = $"Type: {_tagType}";
        if (!string.IsNullOrEmpty(tagName))
        {
            TagNameText.Text = $"Name: {tagName}";
            TagNameText.IsVisible = true;
        }
        else
        {
            TagNameText.IsVisible = false;
        }

        string initialVal = tag switch
        {
            TagNodeByte b => b.Data.ToString(),
            TagNodeShort s => s.Data.ToString(),
            TagNodeInt i => i.Data.ToString(),
            TagNodeLong l => l.Data.ToString(),
            TagNodeFloat f => f.Data.ToString(CultureInfo.InvariantCulture),
            TagNodeDouble d => d.Data.ToString(CultureInfo.InvariantCulture),
            TagNodeString str => str.Data,
            _ => tag.ToString() ?? ""
        };

        ValueTextBox.Text = initialVal;
        ValueTextBox.SelectAll();
        Validate();
    }

    private void ValueTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        Validate();
    }

    private bool Validate()
    {
        if (ValueTextBox == null || OkButton == null || ErrorText == null) return false;

        string input = ValueTextBox.Text ?? string.Empty;
        bool valid = true;
        string error = string.Empty;

        switch (_tagType)
        {
            case TagType.TAG_BYTE:
                if (!sbyte.TryParse(input, out _) && !byte.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a byte value (-128 to 255).";
                }
                break;

            case TagType.TAG_SHORT:
                if (!short.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a 16-bit integer (-32,768 to 32,767).";
                }
                break;

            case TagType.TAG_INT:
                if (!int.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a 32-bit integer (-2,147,483,648 to 2,147,483,647).";
                }
                break;

            case TagType.TAG_LONG:
                if (!long.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a 64-bit integer.";
                }
                break;

            case TagType.TAG_FLOAT:
                if (!float.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _) &&
                    !float.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a valid floating-point number.";
                }
                break;

            case TagType.TAG_DOUBLE:
                if (!double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _) &&
                    !double.TryParse(input, out _))
                {
                    valid = false;
                    error = "Must be a valid double-precision number.";
                }
                break;

            case TagType.TAG_STRING:
                valid = true;
                break;
        }

        ErrorText.Text = error;
        ErrorText.IsVisible = !valid;
        OkButton.IsEnabled = valid;

        return valid;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        ResultValue = ValueTextBox.Text ?? string.Empty;
        ApplyValue();
        Close(true);
    }

    private void ApplyValue()
    {
        string input = ResultValue;
        switch (_tag)
        {
            case TagNodeByte b:
                if (sbyte.TryParse(input, out sbyte sb)) b.Data = (byte)sb;
                else if (byte.TryParse(input, out byte ub)) b.Data = ub;
                break;
            case TagNodeShort s:
                if (short.TryParse(input, out short sv)) s.Data = sv;
                break;
            case TagNodeInt i:
                if (int.TryParse(input, out int iv)) i.Data = iv;
                break;
            case TagNodeLong l:
                if (long.TryParse(input, out long lv)) l.Data = lv;
                break;
            case TagNodeFloat f:
                if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv) ||
                    float.TryParse(input, out fv)) f.Data = fv;
                break;
            case TagNodeDouble d:
                if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv) ||
                    double.TryParse(input, out dv)) d.Data = dv;
                break;
            case TagNodeString str:
                str.Data = input;
                break;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
