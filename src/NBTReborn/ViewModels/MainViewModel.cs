using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBTExplorer.Model;
using NBTModel.Interop;
using Substrate.Core;
using Substrate.Nbt;

namespace NBTReborn.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private bool _showSearchResults = false;

    [ObservableProperty]
    private string _searchResultsHeader = "Search Results";

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    partial void OnSelectedNodeChanged(NodeViewModel? oldValue, NodeViewModel? newValue)
    {
        UpdateToolStates();
    }

    [ObservableProperty]
    private SearchResultViewModel? _selectedSearchResult;

    private int _searchResultIndex = -1;

    public ObservableCollection<NodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();

    #region Tool State Capabilities (Dynamic Toolbar & Menu Enabling)

    public bool CanCut => SelectedNode != null && SelectedNode.CanCut;
    public bool CanCopy => SelectedNode != null && SelectedNode.CanCopy;
    public bool CanPaste => NbtClipboardController.ContainsData &&
        ((SelectedNode != null && SelectedNode.CanPaste) ||
         (SelectedNode?.ParentViewModel != null && SelectedNode.ParentViewModel.CanPaste) ||
         (RootNodes.Count == 1 && RootNodes[0].CanPaste));
    public bool CanRename => SelectedNode != null && SelectedNode.CanRename;
    public bool CanEditValue => SelectedNode != null && (SelectedNode.IsScalar || SelectedNode.CanEdit);
    public bool CanDelete => SelectedNode != null && SelectedNode.CanDelete;
    public bool CanMoveUp => SelectedNode != null && SelectedNode.CanMoveUp;
    public bool CanMoveDown => SelectedNode != null && SelectedNode.CanMoveDown;
    public bool CanRefresh => SelectedNode != null || RootNodes.Count > 0;
    public bool CanSaveAll => RootNodes.Any(r => r.DataNode.IsModified);

    public bool CanAddTag(TagType type) => GetTargetContainerForNewTag(type) != null;

    public bool CanAddByte => CanAddTag(TagType.TAG_BYTE);
    public bool CanAddShort => CanAddTag(TagType.TAG_SHORT);
    public bool CanAddInt => CanAddTag(TagType.TAG_INT);
    public bool CanAddLong => CanAddTag(TagType.TAG_LONG);
    public bool CanAddFloat => CanAddTag(TagType.TAG_FLOAT);
    public bool CanAddDouble => CanAddTag(TagType.TAG_DOUBLE);
    public bool CanAddByteArray => CanAddTag(TagType.TAG_BYTE_ARRAY);
    public bool CanAddIntArray => CanAddTag(TagType.TAG_INT_ARRAY);
    public bool CanAddLongArray => CanAddTag(TagType.TAG_LONG_ARRAY);
    public bool CanAddString => CanAddTag(TagType.TAG_STRING);
    public bool CanAddList => CanAddTag(TagType.TAG_LIST);
    public bool CanAddCompound => CanAddTag(TagType.TAG_COMPOUND);

    public void UpdateToolStates()
    {
        OnPropertyChanged(nameof(CanCut));
        OnPropertyChanged(nameof(CanCopy));
        OnPropertyChanged(nameof(CanPaste));
        OnPropertyChanged(nameof(CanRename));
        OnPropertyChanged(nameof(CanEditValue));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanSaveAll));

        OnPropertyChanged(nameof(CanAddByte));
        OnPropertyChanged(nameof(CanAddShort));
        OnPropertyChanged(nameof(CanAddInt));
        OnPropertyChanged(nameof(CanAddLong));
        OnPropertyChanged(nameof(CanAddFloat));
        OnPropertyChanged(nameof(CanAddDouble));
        OnPropertyChanged(nameof(CanAddByteArray));
        OnPropertyChanged(nameof(CanAddIntArray));
        OnPropertyChanged(nameof(CanAddLongArray));
        OnPropertyChanged(nameof(CanAddString));
        OnPropertyChanged(nameof(CanAddList));
        OnPropertyChanged(nameof(CanAddCompound));
    }

    public bool HasNodes => RootNodes.Count > 0;
    public bool HasNoNodes => RootNodes.Count == 0;

    #endregion

    public MainViewModel()
    {
        RootNodes.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasNodes));
            OnPropertyChanged(nameof(HasNoNodes));
            UpdateToolStates();
        };

        StatusMessage = "Welcome to Redstone NBT — Modern Minecraft NBT Editor";
        UpdateToolStates();
    }

    public void CreateNewNbtFile(string filePath, CompressionType compression = CompressionType.None)
    {
        try
        {
            var node = NbtFileDataNode.CreateNew(filePath, compression, "");
            var vm = new NodeViewModel(node);
            RootNodes.Add(vm);
            SelectedNode = vm;
            vm.IsExpanded = true;
            vm.LoadChildren();
            ShowSearchResults = false;
            SearchResults.Clear();
            StatusMessage = $"Created new NBT file: {Path.GetFileName(filePath)}";
            UpdateToolStates();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating file: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            DataNode? node = null;
            if (RegionFileDataNode.SupportedNamePattern(filePath))
            {
                node = RegionFileDataNode.TryCreateFrom(filePath);
            }
            else
            {
                node = NbtFileDataNode.TryCreateFrom(filePath);
                if (node == null && (filePath.EndsWith(".mca", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".mcr", StringComparison.OrdinalIgnoreCase)))
                {
                    node = RegionFileDataNode.TryCreateFrom(filePath);
                }
            }

            if (node != null)
            {
                var vm = new NodeViewModel(node);
                RootNodes.Add(vm);
                SelectedNode = vm;
                ShowSearchResults = false;
                SearchResults.Clear();
                StatusMessage = $"Opened: {Path.GetFileName(filePath)}";
                UpdateToolStates();
            }
            else
            {
                StatusMessage = $"Could not parse '{Path.GetFileName(filePath)}' as an NBT or Region file.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        try
        {
            var node = new DirectoryDataNode(folderPath);
            var vm = new NodeViewModel(node);
            RootNodes.Add(vm);
            ShowSearchResults = false;
            SearchResults.Clear();
            StatusMessage = $"Opened folder: {Path.GetFileName(folderPath)}";
            UpdateToolStates();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SaveAll()
    {
        int savedCount = 0;
        foreach (var root in RootNodes)
        {
            if (root.DataNode.IsModified)
            {
                root.DataNode.Save();
                savedCount++;
            }
        }

        UpdateToolStates();

        StatusMessage = savedCount > 0
            ? $"Successfully saved {savedCount} modified file(s)."
            : "No modifications to save.";
    }

    [RelayCommand]
    public void RefreshCurrent()
    {
        if (SelectedNode != null)
        {
            SelectedNode.ReloadChildren();
            StatusMessage = $"Refreshed {SelectedNode.DisplayName}";
        }
        else
        {
            foreach (var root in RootNodes)
            {
                root.ReloadChildren();
            }
            StatusMessage = "Refreshed all open roots.";
        }
        UpdateToolStates();
    }

    [RelayCommand]
    public void DeleteSelectedNode()
    {
        if (SelectedNode == null) return;
        if (!SelectedNode.CanDelete)
        {
            StatusMessage = "Selected node cannot be deleted.";
            return;
        }

        var parent = SelectedNode.ParentViewModel;
        string name = SelectedNode.DisplayName;
        if (SelectedNode.DataNode.DeleteNode())
        {
            parent?.Children.Remove(SelectedNode);
            parent?.RefreshDisplay();
            SelectedNode = null;
            StatusMessage = $"Deleted: {name}";
            UpdateToolStates();
        }
    }

    [RelayCommand]
    public void CutSelectedNode()
    {
        if (SelectedNode == null || !SelectedNode.CanCut) return;

        var parent = SelectedNode.ParentViewModel;
        string name = SelectedNode.DisplayName;
        if (SelectedNode.DataNode.CutNode())
        {
            parent?.Children.Remove(SelectedNode);
            parent?.RefreshDisplay();
            SelectedNode = null;
            StatusMessage = $"Cut: {name}";
            UpdateToolStates();
        }
    }

    [RelayCommand]
    public void CopySelectedNode()
    {
        if (SelectedNode == null || !SelectedNode.CanCopy) return;

        if (SelectedNode.DataNode.CopyNode())
        {
            StatusMessage = $"Copied: {SelectedNode.DisplayName}";
            UpdateToolStates();
        }
    }

    [RelayCommand]
    public void PasteIntoSelectedNode()
    {
        var target = SelectedNode;
        if (target != null && !target.CanPaste)
        {
            target = target.ParentViewModel;
        }

        if (target != null && target.CanPaste)
        {
            if (target.DataNode.PasteNode())
            {
                target.IsExpanded = true;
                target.ReloadChildren();
                StatusMessage = $"Pasted tag into {target.DisplayName}";
                UpdateToolStates();
            }
        }
        else
        {
            StatusMessage = "Cannot paste into selected node (clipboard empty or incompatible tag).";
        }
    }

    [RelayCommand]
    public void MoveSelectedNodeUp()
    {
        if (SelectedNode == null || !SelectedNode.CanMoveUp) return;
        var parent = SelectedNode.ParentViewModel;
        if (SelectedNode.DataNode.ChangeRelativePosition(-1))
        {
            parent?.ReloadChildren();
            StatusMessage = "Moved tag up.";
            UpdateToolStates();
        }
    }

    [RelayCommand]
    public void MoveSelectedNodeDown()
    {
        if (SelectedNode == null || !SelectedNode.CanMoveDown) return;
        var parent = SelectedNode.ParentViewModel;
        if (SelectedNode.DataNode.ChangeRelativePosition(1))
        {
            parent?.ReloadChildren();
            StatusMessage = "Moved tag down.";
            UpdateToolStates();
        }
    }

    [RelayCommand]
    public void EditSelectedNode()
    {
        if (SelectedNode == null) return;
        if (SelectedNode.IsScalar)
        {
            SelectedNode.BeginEdit();
        }
        else if (SelectedNode.IsContainer)
        {
            SelectedNode.IsExpanded = !SelectedNode.IsExpanded;
        }
    }

    public NodeViewModel? GetTargetContainerForNewTag(TagType type)
    {
        if (SelectedNode != null)
        {
            if (SelectedNode.IsContainer)
            {
                // If a container is explicitly selected, it must directly support creating that tag type
                return SelectedNode.CanCreateTag(type) ? SelectedNode : null;
            }

            // If a non-container (e.g. scalar tag) is selected, check if its parent container can accept the tag as a sibling
            if (SelectedNode.ParentViewModel != null && SelectedNode.ParentViewModel.IsContainer && SelectedNode.ParentViewModel.CanCreateTag(type))
            {
                return SelectedNode.ParentViewModel;
            }

            return null;
        }

        // If nothing is selected, but exactly one root container exists, allow adding to that root
        if (RootNodes.Count == 1 && RootNodes[0].IsContainer && RootNodes[0].CanCreateTag(type))
        {
            return RootNodes[0];
        }

        return null;
    }

    public void AddCreatedTag(NodeViewModel targetContainer, TagNode tag, string name)
    {
        if (targetContainer.DataNode is TagCompoundDataNode compoundNode)
        {
            compoundNode.AddTag(tag, name);
        }
        else if (targetContainer.DataNode is TagListDataNode listNode)
        {
            listNode.AppendTag(tag);
        }
        else if (targetContainer.DataNode is NbtFileDataNode fileNode)
        {
            fileNode.AddTag(tag, name);
        }
        else
        {
            return;
        }

        targetContainer.IsExpanded = true;
        targetContainer.ReloadChildren();
        StatusMessage = $"Added {tag.GetTagType()} tag: {(string.IsNullOrEmpty(name) ? "" : name)}";
        UpdateToolStates();
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        StatusMessage = $"Searching for \"{SearchQuery}\"...";
        SearchResults.Clear();
        _searchResultIndex = -1;

        await Task.Run(() =>
        {
            foreach (var root in RootNodes)
            {
                SearchRecursive(root.DataNode, SearchQuery, root.DisplayName);
            }
        });

        IsSearching = false;
        ShowSearchResults = SearchResults.Count > 0;
        SearchResultsHeader = $"Search Results ({SearchResults.Count})";
        StatusMessage = $"Search complete: Found {SearchResults.Count} match(es).";

        if (SearchResults.Count > 0)
        {
            FindNext();
        }
    }

    [RelayCommand]
    public void CloseSearchResults()
    {
        ShowSearchResults = false;
    }

    [RelayCommand]
    public void FindNext()
    {
        if (SearchResults.Count == 0)
        {
            StatusMessage = "No search results.";
            return;
        }

        _searchResultIndex = (_searchResultIndex + 1) % SearchResults.Count;
        SelectedSearchResult = SearchResults[_searchResultIndex];
    }

    partial void OnSelectedSearchResultChanged(SearchResultViewModel? value)
    {
        if (value?.Node == null) return;
        NavigateToNode(value.Node);
    }

    public NodeViewModel? NavigateToNode(DataNode targetNode)
    {
        foreach (var root in RootNodes)
        {
            var foundVm = root.FindOrExpandTo(targetNode);
            if (foundVm != null)
            {
                if (SelectedNode != null)
                {
                    SelectedNode.IsSelected = false;
                }

                foundVm.IsSelected = true;
                SelectedNode = foundVm;
                StatusMessage = $"Jumped to: {foundVm.DisplayName}";
                return foundVm;
            }
        }
        return null;
    }

    private void SearchRecursive(DataNode node, string query, string currentPath)
    {
        if (node.NodeDisplay != null && node.NodeDisplay.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SearchResults.Add(new SearchResultViewModel
                {
                    Path = currentPath,
                    MatchText = node.NodeDisplay,
                    Node = node
                });
            });
        }

        node.Expand();
        foreach (DataNode child in node.Nodes)
        {
            string nextPath = $"{currentPath} > {child.NodeDisplay}";
            SearchRecursive(child, query, nextPath);
        }
    }
}

public class SearchResultViewModel
{
    public string Path { get; set; } = string.Empty;
    public string MatchText { get; set; } = string.Empty;
    public DataNode? Node { get; set; }
}
