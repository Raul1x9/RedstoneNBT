using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTExplorer.Model;
using Substrate.Nbt;

namespace NBTReborn.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    private readonly DataNode _dataNode;
    private bool _isLoaded = false;

    public DataNode DataNode => _dataNode;
    public NodeViewModel? ParentViewModel { get; set; }

    public ObservableCollection<NodeViewModel> Children { get; } = new();

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _nodeType = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public Bitmap? Icon { get; private set; }

    public bool CanEdit => _dataNode.CanEditNode;
    public bool CanRename => _dataNode.CanRenameNode;
    public bool CanDelete => _dataNode.CanDeleteNode;
    public bool CanCopy => _dataNode.CanCopyNode;
    public bool CanCut => _dataNode.CanCutNode;
    public bool CanPaste => _dataNode.CanPasteIntoNode;
    public bool IsContainer => _dataNode.IsContainerType;
    public bool CanMoveUp => _dataNode.CanMoveNodeUp;
    public bool CanMoveDown => _dataNode.CanMoveNodeDown;

    public bool CanCreateTag(TagType type) => _dataNode.CanCreateTag(type);

    public NodeViewModel(DataNode dataNode, NodeViewModel? parent = null)
    {
        _dataNode = dataNode;
        ParentViewModel = parent;
        DisplayName = dataNode.NodeDisplay;
        NodeType = dataNode.GetType().Name;
        LoadIcon();

        // If the node has children or is an expandable container, add dummy placeholder for chevron
        if (NodeCanHaveChildren(dataNode))
        {
            Children.Add(new DummyNodeViewModel());
        }
    }

    public static bool NodeCanHaveChildren(DataNode node)
    {
        if (node == null) return false;
        if (node.Nodes.Count > 0) return true;
        if (node.HasUnexpandedChildren) return true;
        if (node is TagDataNode.Container container)
        {
            return container.TagCount > 0;
        }
        if (node is RegionFileDataNode)
        {
            return true;
        }
        if (node is RegionChunkDataNode)
        {
            return true;
        }
        if (node is DirectoryDataNode || node is NbtFileDataNode)
        {
            return true;
        }
        return false;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_isLoaded)
        {
            LoadChildren();
        }
    }

    public void LoadChildren()
    {
        if (_isLoaded) return;
        _isLoaded = true;

        Children.Clear();
        _dataNode.Expand();

        foreach (DataNode child in _dataNode.Nodes)
        {
            Children.Add(new NodeViewModel(child, this));
        }
    }

    public void ReloadChildren()
    {
        _isLoaded = false;
        Children.Clear();
        LoadChildren();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        DisplayName = _dataNode.NodeDisplay;
        ParentViewModel?.RefreshDisplay();
    }

    /// <summary>
    /// Finds or lazily creates child ViewModels down to the target DataNode.
    /// </summary>
    public NodeViewModel? FindOrExpandTo(DataNode target)
    {
        if (_dataNode == target)
        {
            return this;
        }

        // Expand this node so its children are loaded
        IsExpanded = true;
        LoadChildren();

        foreach (var child in Children)
        {
            if (child is DummyNodeViewModel) continue;

            if (child.DataNode == target)
            {
                return child;
            }

            // Check if target is a descendant of child
            if (IsDescendantOf(target, child.DataNode))
            {
                return child.FindOrExpandTo(target);
            }
        }

        return null;
    }

    private static bool IsDescendantOf(DataNode target, DataNode potentialAncestor)
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current == potentialAncestor) return true;
            current = current.Parent;
        }
        return false;
    }

    private void LoadIcon()
    {
        string iconName = "document-attribute-b.png";

        if (_dataNode is RegionFileDataNode) iconName = "wooden-box.png";
        else if (_dataNode is RegionChunkDataNode) iconName = "box.png";
        else if (_dataNode is DirectoryDataNode) iconName = "folder-open.png";
        else if (_dataNode is NbtFileDataNode) iconName = "box.png";
        else if (_dataNode is TagDataNode tagNode)
        {
            switch (tagNode.Tag.GetTagType())
            {
                case TagType.TAG_COMPOUND: iconName = "folder-open.png"; break;
                case TagType.TAG_LIST: iconName = "edit-list.png"; break;
                case TagType.TAG_BYTE: iconName = "document-attribute-b.png"; break;
                case TagType.TAG_SHORT: iconName = "document-attribute-s.png"; break;
                case TagType.TAG_INT: iconName = "document-attribute-i.png"; break;
                case TagType.TAG_LONG: iconName = "document-attribute-l.png"; break;
                case TagType.TAG_FLOAT: iconName = "document-attribute-f.png"; break;
                case TagType.TAG_DOUBLE: iconName = "document-attribute-d.png"; break;
                case TagType.TAG_BYTE_ARRAY: iconName = "edit-code-b.png"; break;
                case TagType.TAG_STRING: iconName = "edit-small-caps.png"; break;
                case TagType.TAG_INT_ARRAY: iconName = "edit-code-i.png"; break;
                case TagType.TAG_LONG_ARRAY: iconName = "edit-code-l.png"; break;
                default: iconName = "document-attribute-b.png"; break;
            }
        }

        try
        {
            var uri = new System.Uri($"avares://NBTReborn/Assets/{iconName}");
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                Icon = new Bitmap(stream);
            }
        }
        catch
        {
            // Graceful fallback if asset cannot be loaded
        }
    }
}

public class DummyNodeViewModel : NodeViewModel
{
    public DummyNodeViewModel() : base(new DummyDataNode()) { }
}

public class DummyDataNode : DataNode
{
    public override string NodeDisplay => "Loading...";
    public override bool HasUnexpandedChildren => false;
    protected override void ExpandCore() { }
    protected override void ReleaseCore() { }
    protected override void SaveCore() { }
}
