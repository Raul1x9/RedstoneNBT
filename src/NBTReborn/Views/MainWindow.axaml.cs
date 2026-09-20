using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NBTExplorer.Model;
using NBTReborn.ViewModels;
using Substrate.Nbt;

namespace NBTReborn.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    #region Search Navigation & Auto-Scrolling

    private async void SearchResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SearchResultsListBox?.SelectedItem is SearchResultViewModel res && res.Node != null && DataContext is MainViewModel vm)
        {
            var foundVm = vm.NavigateToNode(res.Node);
            if (foundVm != null)
            {
                await ScrollNodeIntoViewAsync(foundVm);
            }
        }
    }

    public async Task ScrollNodeIntoViewAsync(NodeViewModel target)
    {
        // 1. Build hierarchy of ancestors down to target
        var path = new List<NodeViewModel>();
        var curr = target;
        while (curr != null)
        {
            path.Insert(0, curr);
            curr = curr.ParentViewModel;
        }

        // 2. Expand all ancestors
        for (int i = 0; i < path.Count - 1; i++)
        {
            path[i].IsExpanded = true;
            path[i].LoadChildren();
        }

        // Give Avalonia layout pass to create containers
        await Task.Delay(40);

        // 3. Bring the item control into view
        Control? currentContainer = MainTreeView;
        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            TreeViewItem? itemControl = null;

            for (int retry = 0; retry < 6; retry++)
            {
                if (currentContainer is ItemsControl ic)
                {
                    itemControl = ic.ContainerFromItem(node) as TreeViewItem;
                }
                if (itemControl != null) break;
                await Task.Delay(25);
            }

            if (itemControl != null)
            {
                itemControl.BringIntoView();
                currentContainer = itemControl;
            }
        }
    }

    #endregion

    #region File Menu & Toolbar Operations

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Minecraft NBT or Region File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Minecraft Region / NBT Files")
                {
                    Patterns = new[] { "*.mca", "*.mcr", "*.dat", "*.nbt" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0 && DataContext is MainViewModel vm)
        {
            var path = files[0].Path.LocalPath;
            vm.OpenFile(path);
        }
    }

    private async void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Directory / World Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            var path = folders[0].Path.LocalPath;
            vm.OpenFolder(path);
        }
    }

    private void OpenMinecraftSaveFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        string savesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft", "saves");

        if (OperatingSystem.IsWindows())
        {
            savesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "saves");
        }
        else if (OperatingSystem.IsMacOS())
        {
            savesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft", "saves");
        }

        if (Directory.Exists(savesDir))
        {
            vm.OpenFolder(savesDir);
        }
        else
        {
            OpenFolder_Click(sender, e);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveAll();
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RefreshCurrent();
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Edit Operations (Rename, Edit Value, Delete, Cut, Copy, Paste, Move)

    private async void EditValue_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedNode == null) return;
        var sel = vm.SelectedNode;

        if (sel.DataNode is TagDataNode tagNode)
        {
            // Container nodes toggle expansion on edit command
            if (tagNode is TagCompoundDataNode || tagNode is TagListDataNode)
            {
                sel.IsExpanded = !sel.IsExpanded;
                return;
            }

            // Array nodes (Byte, Int, Long arrays)
            if (tagNode is TagByteArrayDataNode || tagNode is TagIntArrayDataNode || tagNode is TagLongArrayDataNode)
            {
                var arrayDlg = new EditByteArrayWindow(tagNode.Tag, tagNode.NodeName);
                if (await arrayDlg.ShowDialog<bool>(this))
                {
                    tagNode.SetModified();
                    sel.RefreshDisplay();
                    vm.StatusMessage = $"Updated array: {tagNode.NodeName ?? "data"}";
                }
                return;
            }

            // Scalar tags (Byte, Short, Int, Long, Float, Double, String)
            var dlg = new EditValueWindow(tagNode.Tag, tagNode.NodeName);
            if (await dlg.ShowDialog<bool>(this))
            {
                tagNode.SetModified();
                sel.RefreshDisplay();
                vm.StatusMessage = $"Updated value: {tagNode.NodeName ?? "tag"}";
            }
        }
    }

    private async void Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedNode == null) return;
        var sel = vm.SelectedNode;

        if (!sel.CanRename)
        {
            vm.StatusMessage = "Selected tag cannot be renamed (unnamed or unsupported container).";
            return;
        }

        var parentCompound = sel.ParentViewModel?.DataNode as TagCompoundDataNode;
        var existingNames = parentCompound?.NamedTagContainer?.TagNamesInUse ?? Enumerable.Empty<string>();

        var dlg = new RenameTagWindow(sel.DataNode.NodeName, existingNames);
        if (await dlg.ShowDialog<bool>(this))
        {
            string newName = dlg.ResultName;
            if (parentCompound != null && sel.DataNode is TagDataNode tagNode)
            {
                if (parentCompound.NamedTagContainer.RenameTag(tagNode.Tag, newName))
                {
                    parentCompound.SetModified();
                    sel.RefreshDisplay();
                    sel.ParentViewModel?.ReloadChildren();
                    vm.StatusMessage = $"Renamed tag to '{newName}'.";
                }
            }
        }
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.DeleteSelectedNode();
        }
    }

    private void Cut_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CutSelectedNode();
        }
    }

    private void Copy_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CopySelectedNode();
        }
    }

    private void Paste_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PasteIntoSelectedNode();
        }
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.MoveSelectedNodeUp();
        }
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.MoveSelectedNodeDown();
        }
    }

    #endregion

    #region Tag Creation

    private async Task CreateTag(TagType type)
    {
        if (DataContext is not MainViewModel vm) return;

        var target = vm.GetTargetContainerForNewTag(type);
        if (target == null)
        {
            vm.StatusMessage = $"Select a Compound or List tag to add a {type} tag.";
            return;
        }

        bool hasName = target.DataNode is TagCompoundDataNode || target.DataNode is NbtFileDataNode;
        IEnumerable<string>? existingNames = null;
        if (target.DataNode is TagCompoundDataNode comp)
        {
            existingNames = comp.NamedTagContainer.TagNamesInUse;
        }

        var dlg = new CreateTagWindow(type, hasName, existingNames);
        if (await dlg.ShowDialog<bool>(this) && dlg.ResultTag != null)
        {
            vm.AddCreatedTag(target, dlg.ResultTag, dlg.ResultName);
        }
    }

    private async void AddByte_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_BYTE);
    private async void AddShort_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_SHORT);
    private async void AddInt_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_INT);
    private async void AddLong_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_LONG);
    private async void AddFloat_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_FLOAT);
    private async void AddDouble_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_DOUBLE);
    private async void AddByteArray_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_BYTE_ARRAY);
    private async void AddIntArray_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_INT_ARRAY);
    private async void AddLongArray_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_LONG_ARRAY);
    private async void AddString_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_STRING);
    private async void AddList_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_LIST);
    private async void AddCompound_Click(object? sender, RoutedEventArgs e) => await CreateTag(TagType.TAG_COMPOUND);

    #endregion

    #region Search, Shortcuts & Tree Interactions

    private void FocusSearchBox_Click(object? sender, RoutedEventArgs e)
    {
        SearchTextBox?.Focus();
        SearchTextBox?.SelectAll();
    }

    private void FindNext_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.FindNext();
        }
    }

    private void About_Click(object? sender, RoutedEventArgs e)
    {
        var about = new AboutWindow();
        about.ShowDialog(this);
    }

    private async void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            await vm.SearchAsync();
        }
    }

    private void MainTreeView_DoubleTapped(object? sender, TappedEventArgs e)
    {
        EditValue_Click(sender, e);
    }

    private void MainTreeView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            Delete_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            Rename_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            EditValue_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.Control)
        {
            if (e.Key == Key.E) { EditValue_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.R) { Rename_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.C) { Copy_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.X) { Cut_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.V) { Paste_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.Up) { MoveUp_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.Down) { MoveDown_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.S) { Save_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.O) { OpenFile_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.F) { FocusSearchBox_Click(sender, e); e.Handled = true; }
        }
        else if (e.Key == Key.F5)
        {
            Refresh_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.F3)
        {
            FindNext_Click(sender, e);
            e.Handled = true;
        }
    }

    #endregion
}