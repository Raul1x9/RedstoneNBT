using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RedstoneNBT.Views;

public partial class RenameTagWindow : Window
{
    private readonly string _originalName;
    private readonly HashSet<string> _existingNames;

    public string ResultName { get; private set; } = string.Empty;

    public RenameTagWindow()
    {
        InitializeComponent();
        _originalName = string.Empty;
        _existingNames = new HashSet<string>(StringComparer.Ordinal);
    }

    public RenameTagWindow(string currentName, IEnumerable<string>? existingNames = null)
    {
        InitializeComponent();
        _originalName = currentName ?? string.Empty;
        _existingNames = existingNames != null
            ? new HashSet<string>(existingNames, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        NameTextBox.Text = _originalName;
        NameTextBox.SelectAll();
        Validate();
    }

    private void NameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        Validate();
    }

    private bool Validate()
    {
        if (NameTextBox == null || OkButton == null || ErrorText == null) return false;

        string input = NameTextBox.Text?.Trim() ?? string.Empty;
        bool valid = true;
        string error = string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            valid = false;
            error = "Tag name cannot be empty.";
        }
        else if (!string.Equals(input, _originalName, StringComparison.Ordinal) && _existingNames.Contains(input))
        {
            valid = false;
            error = $"A tag named '{input}' already exists in this container.";
        }

        ErrorText.Text = error;
        ErrorText.IsVisible = !valid;
        OkButton.IsEnabled = valid;

        return valid;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        ResultName = NameTextBox.Text?.Trim() ?? string.Empty;
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
