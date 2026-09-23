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
using RedstoneNBT.ViewModels;
using Substrate.Core;
using Substrate.Nbt;

namespace RedstoneNBT.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Window_Drop, Avalonia.Interactivity.RoutingStrategies.Bubble | Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        AddHandler(DragDrop.DragOverEvent, Window_DragOver, Avalonia.Interactivity.RoutingStrategies.Bubble | Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
    }

    #region Drag and Drop File Opening

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Window_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var items = new List<IStorageItem>();
        var files = e.DataTransfer.TryGetFiles();
        if (files != null && files.Length > 0)
        {
            items.AddRange(files);
        }
        else
        {
            var single = e.DataTransfer.TryGetFile();
            if (single != null) items.Add(single);
        }

        if (items.Count > 0)
        {
            int openedCount = 0;
            string lastOpened = string.Empty;

            foreach (var item in items)
            {
                string? path = item.TryGetLocalPath();
                if (string.IsNullOrEmpty(path) && item.Path != null && item.Path.IsFile)
                {
                    path = item.Path.LocalPath;
                }

                if (string.IsNullOrEmpty(path)) continue;

                if (Directory.Exists(path))
                {
                    vm.OpenFolder(path);
                    openedCount++;
                    lastOpened = Path.GetFileName(path);
                }
                else if (File.Exists(path))
                {
                    vm.OpenFile(path);
                    openedCount++;
                    lastOpened = Path.GetFileName(path);
                }
            }

            if (openedCount > 1)
            {
                vm.StatusMessage = $"Opened {openedCount} items from drag & drop.";
            }
            else if (openedCount == 1)
            {
                vm.StatusMessage = $"Opened: {lastOpened}";
            }

            e.Handled = true;
        }
    }

    #endregion

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
        var path = new List<NodeViewModel>();
        var curr = target;
        while (curr != null)
        {
            path.Insert(0, curr);
            curr = curr.ParentViewModel;
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            path[i].IsExpanded = true;
            path[i].LoadChildren();
        }

        await Task.Delay(40);

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

    #region Inline In-Place Value Editing

    private void ValueText_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is NodeViewModel vm && vm.IsScalar)
        {
            vm.BeginEdit();
            e.Handled = true;
        }
    }

    private void InlineEdit_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void InlineEdit_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is NodeViewModel vm)
        {
            if (e.Key == Key.Enter)
            {
                if (vm.CommitEdit(tb.Text ?? string.Empty))
                {
                    if (DataContext is MainViewModel mainVm)
                    {
                        mainVm.StatusMessage = $"Updated value: {vm.DisplayName}";
                        mainVm.UpdateToolStates();
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.CancelEdit();
                e.Handled = true;
            }
        }
    }

    private void InlineEdit_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is NodeViewModel vm && vm.IsEditing)
        {
            if (vm.CommitEdit(tb.Text ?? string.Empty))
            {
                if (DataContext is MainViewModel mainVm)
                {
                    mainVm.StatusMessage = $"Updated value: {vm.DisplayName}";
                    mainVm.UpdateToolStates();
                }
            }
        }
    }

    #endregion

    #region File Menu & Toolbar Operations

    private async void NewFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        if (DataContext is not MainViewModel vm) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Create New NBT File",
            DefaultExtension = "nbt",
            SuggestedFileName = "new.nbt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Uncompressed NBT File (*.nbt)")
                {
                    Patterns = new[] { "*.nbt" }
                },
                new FilePickerFileType("GZip-compressed NBT File (*.dat, *.nbt)")
                {
                    Patterns = new[] { "*.dat", "*.nbt" }
                },
                new FilePickerFileType("Zlib-compressed NBT File (*.nbt, *.dat)")
                {
                    Patterns = new[] { "*.nbt", "*.dat" }
                },
                FilePickerFileTypes.All
            }
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            CompressionType compression = CompressionType.None;
            if (path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
            {
                compression = CompressionType.GZip;
            }

            vm.CreateNewNbtFile(path, compression);
        }
    }

    private async void SaveAs_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        if (DataContext is not MainViewModel vm) return;

        NodeViewModel? targetNode = null;
        if (vm.SelectedNode != null)
        {
            var cur = vm.SelectedNode;
            while (cur != null)
            {
                if (cur.DataNode is NbtFileDataNode)
                {
                    targetNode = cur;
                    break;
                }
                cur = cur.ParentViewModel;
            }
        }

        if (targetNode == null && vm.RootNodes.Count > 0)
        {
            targetNode = vm.RootNodes[0];
        }

        if (targetNode?.DataNode is not NbtFileDataNode fileNode)
        {
            vm.StatusMessage = "Please select an NBT file to Save As.";
            return;
        }

        var ext = Path.GetExtension(fileNode.NodeName).TrimStart('.');
        if (string.IsNullOrEmpty(ext)) ext = "nbt";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save NBT File As",
            DefaultExtension = ext,
            SuggestedFileName = fileNode.NodeName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("GZip-compressed NBT File (*.dat, *.nbt)")
                {
                    Patterns = new[] { "*.dat", "*.nbt" }
                },
                new FilePickerFileType("Uncompressed NBT File (*.nbt)")
                {
                    Patterns = new[] { "*.nbt" }
                },
                new FilePickerFileType("Zlib-compressed NBT File (*.nbt, *.dat)")
                {
                    Patterns = new[] { "*.nbt", "*.dat" }
                },
                FilePickerFileTypes.All
            }
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            CompressionType compression = path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
                ? CompressionType.GZip
                : fileNode.Compression;

            fileNode.SaveAs(path, compression);
            targetNode.RefreshDisplay();
            vm.StatusMessage = $"Saved file as: {Path.GetFileName(path)}";
            vm.UpdateToolStates();
        }
    }

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
                new FilePickerFileType("All Supported Files (*.dat, *.nbt, *.mca, *.mcr, *.schematic, *.schem, *.mcstructure)")
                {
                    Patterns = new[] { "*.dat", "*.nbt", "*.mca", "*.mcr", "*.schematic", "*.schem", "*.mcstructure", "*.dat_mcr", "*.dat_old", "*.bpt", "*.rc", "*.2dr" }
                },
                new FilePickerFileType("GZip-compressed NBT File (*.dat, *.nbt)")
                {
                    Patterns = new[] { "*.dat", "*.nbt" }
                },
                new FilePickerFileType("Uncompressed NBT File (*.nbt)")
                {
                    Patterns = new[] { "*.nbt" }
                },
                new FilePickerFileType("Minecraft Region Files (*.mca, *.mcr)")
                {
                    Patterns = new[] { "*.mca", "*.mcr" }
                },
                new FilePickerFileType("Schematic Files (*.schematic, *.schem)")
                {
                    Patterns = new[] { "*.schematic", "*.schem" }
                },
                new FilePickerFileType("Bedrock Structure Files (*.mcstructure)")
                {
                    Patterns = new[] { "*.mcstructure" }
                },
                new FilePickerFileType("Zlib-compressed NBT File (*.nbt, *.dat)")
                {
                    Patterns = new[] { "*.nbt", "*.dat" }
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

    #region Edit Operations (Inline Edit, Rename, Delete, Cut, Copy, Paste, Move)

    private async void EditValue_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedNode == null) return;
        var sel = vm.SelectedNode;

        // Inline in-place editing for scalar values (no popup window!)
        if (sel.IsScalar)
        {
            sel.BeginEdit();
            return;
        }

        // Containers toggle expansion
        if (sel.IsContainer)
        {
            sel.IsExpanded = !sel.IsExpanded;
            return;
        }

        // Array nodes (Byte, Int, Long arrays) use dedicated array editor dialog
        if (sel.DataNode is TagDataNode tagNode &&
            (tagNode is TagByteArrayDataNode || tagNode is TagIntArrayDataNode || tagNode is TagLongArrayDataNode))
        {
            var arrayDlg = new EditByteArrayWindow(tagNode.Tag, tagNode.NodeName);
            if (await arrayDlg.ShowDialog<bool>(this))
            {
                tagNode.SetModified();
                sel.RefreshDisplay();
                vm.StatusMessage = $"Updated array: {tagNode.NodeName ?? "data"}";
                vm.UpdateToolStates();
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
                    vm.UpdateToolStates();
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
        else if (target.DataNode is NbtFileDataNode fileNode)
        {
            existingNames = fileNode.NamedTagContainer.TagNamesInUse;
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
        ToolbarSearchBox?.Focus();
        ToolbarSearchBox?.SelectAll();
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

    private void ExpandAll_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var targetNodes = vm.SelectedNode != null 
            ? new[] { vm.SelectedNode } 
            : vm.RootNodes.ToArray();

        foreach (var node in targetNodes)
        {
            ExpandRecursive(node, 0, 5);
        }
    }

    private static void ExpandRecursive(NodeViewModel node, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        node.IsExpanded = true;
        node.LoadChildren();
        foreach (var child in node.Children)
        {
            if (child is not DummyNodeViewModel)
            {
                ExpandRecursive(child, depth + 1, maxDepth);
            }
        }
    }

    private void CollapseAll_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var targetNodes = vm.SelectedNode != null 
            ? new[] { vm.SelectedNode } 
            : vm.RootNodes.ToArray();

        foreach (var node in targetNodes)
        {
            CollapseRecursive(node);
        }
    }

    private static void CollapseRecursive(NodeViewModel node)
    {
        foreach (var child in node.Children)
        {
            if (child is not DummyNodeViewModel)
            {
                CollapseRecursive(child);
            }
        }
        node.IsExpanded = false;
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
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            if (e.Key == Key.S) { SaveAs_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.O) { OpenFolder_Click(sender, e); e.Handled = true; }
        }
        else if (e.KeyModifiers == KeyModifiers.Control)
        {
            if (e.Key == Key.N) { NewFile_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.E) { EditValue_Click(sender, e); e.Handled = true; }
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