using System.IO;
using System.Text.RegularExpressions;
using Substrate.Core;
using Substrate.Nbt;
using System.Collections.Generic;
using System;
using NBTModel.Interop;

namespace NBTExplorer.Model
{
    public class NbtFileDataNode : DataNode, IMetaTagContainer
    {
        private NbtTree _tree;
        private string _path;
        private CompressionType _compressionType;

        private CompoundTagContainer _container;

        private static Regex _namePattern = new Regex(@"\.(dat|nbt|schematic|schem|mcstructure|dat_mcr|dat_old|bpt|rc|dump|raw)$", RegexOptions.IgnoreCase);

        public string FilePath => _path;
        public CompressionType Compression => _compressionType;

        private NbtFileDataNode (string path, CompressionType compressionType)
        {
            _path = path;
            _compressionType = compressionType;
            _container = new CompoundTagContainer(new TagNodeCompound());
        }

        public static NbtFileDataNode CreateNew (string path, CompressionType compressionType, string rootName = "")
        {
            var node = new NbtFileDataNode(path, compressionType);
            node._tree = new NbtTree(new TagNodeCompound(), rootName);
            node._container = new CompoundTagContainer(node._tree.Root);
            node.SaveCore();
            return node;
        }

        public void SaveAs (string newPath, CompressionType compressionType)
        {
            if (_tree == null) {
                ExpandCore();
            }
            _path = newPath;
            _compressionType = compressionType;
            SaveCore();
            IsDataModified = false;
        }

        public static NbtFileDataNode TryCreateFrom (string path)
        {
            if (!File.Exists(path))
                return null;

            try {
                byte[] header = new byte[8];
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    int read = fs.Read(header, 0, Math.Min((int)fs.Length, 8));
                }

                // GZip magic: 0x1F 0x8B
                if (header.Length >= 2 && header[0] == 0x1F && header[1] == 0x8B) {
                    return TryCreateFrom(path, CompressionType.GZip);
                }

                // Zlib magic: 0x78
                if (header.Length >= 2 && header[0] == 0x78 && (header[0] * 256 + header[1]) % 31 == 0) {
                    return TryCreateFrom(path, CompressionType.Zlib)
                        ?? TryCreateFrom(path, CompressionType.Deflate);
                }

                // Uncompressed Java NBT (TAG_Compound = 0x0A)
                if (header.Length >= 1 && header[0] == 0x0A) {
                    return TryCreateFrom(path, CompressionType.None);
                }
            }
            catch { }

            return TryCreateFrom(path, CompressionType.GZip)
                ?? TryCreateFrom(path, CompressionType.Zlib)
                ?? TryCreateFrom(path, CompressionType.None)
                ?? TryCreateFrom(path, CompressionType.Deflate);
        }

        private static NbtFileDataNode TryCreateFrom (string path, CompressionType compressionType)
        {
            try {
                NBTFile file = new NBTFile(path);
                NbtTree tree = new NbtTree();
                tree.ReadFrom(file.GetDataInputStream(compressionType));

                if (tree.Root == null)
                    return null;

                var node = new NbtFileDataNode(path, compressionType);
                node._tree = tree;
                node._container = new CompoundTagContainer(tree.Root);
                return node;
            }
            catch {
                return null;
            }
        }

        public static bool SupportedNamePattern (string path)
        {
            path = Path.GetFileName(path);
            return _namePattern.IsMatch(path);
        }

        protected override NodeCapabilities Capabilities
        {
            get
            {
                return NodeCapabilities.CreateTag
                    | NodeCapabilities.PasteInto
                    | NodeCapabilities.Search
                    | NodeCapabilities.Refresh
                    | NodeCapabilities.Rename;
            }
        }

        public override string NodeName
        {
            get { return Path.GetFileName(_path); }
        }

        public override string NodePathName
        {
            get { return Path.GetFileName(_path); }
        }

        public override string NodeDisplay
        {
            get
            {
                if (_tree != null && _tree.Root != null) {
                    if (!string.IsNullOrEmpty(_tree.Name))
                        return NodeName + " [" + _tree.Name + ": " + _tree.Root.Count + " entries]";
                    else
                        return NodeName + " [" + _tree.Root.Count + " entries]";
                }
                else
                    return NodeName;
            }
        }

        public override bool HasUnexpandedChildren
        {
            get { return !IsExpanded; }
        }

        public override bool IsContainerType
        {
            get { return true; }
        }

        protected override void ExpandCore ()
        {
            if (_tree == null) {
                NBTFile file = new NBTFile(_path);
                _tree = new NbtTree();
                _tree.ReadFrom(file.GetDataInputStream(_compressionType));

                if (_tree.Root != null) {
                    _container = new CompoundTagContainer(_tree.Root);
                }
            }

            var list = new SortedList<TagKey, TagNode>();
            foreach (var item in _tree.Root) {
                list.Add(new TagKey(item.Key, item.Value.GetTagType()), item.Value);
            }

            foreach (TagNode tag in list.Values) {
                TagDataNode node = TagDataNode.CreateFromTag(tag);
                if (node != null)
                    Nodes.Add(node);
            }
        }

        protected override void ReleaseCore ()
        {
            _tree = null;
            Nodes.Clear();
        }

        protected override void SaveCore ()
        {
            NBTFile file = new NBTFile(_path);
            using (Stream str = file.GetDataOutputStream(_compressionType)) {
                _tree.WriteTo(str);
            }
        }

        public override bool RefreshNode ()
        {
            Dictionary<string, object> expandSet = BuildExpandSet(this);
            Release();
            RestoreExpandSet(this, expandSet);

            return expandSet != null;
        }

        public override bool CanRenameNode
        {
            get { return _tree != null; }
        }

        public override bool RenameNode ()
        {
            if (CanRenameNode && FormRegistry.EditString != null) {
                RestrictedStringFormData data = new RestrictedStringFormData(_tree.Name ?? "") {
                    AllowEmpty = true,
                };

                if (FormRegistry.RenameTag(data)) {
                    if (_tree.Name != data.Value) {
                        _tree.Name = data.Value;
                        IsDataModified = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool CanCreateTag (TagType type)
        {
            if (_tree == null) {
                ExpandCore();
            }
            return _tree != null && _tree.Root != null && Enum.IsDefined(typeof(TagType), type) && type != TagType.TAG_END;
        }

        public override bool CanPasteIntoNode
        {
            get { return _tree != null && _tree.Root != null && NbtClipboardController.ContainsData; }
        }

        public override bool CreateNode (TagType type)
        {
            if (!CanCreateTag(type))
                return false;

            if (FormRegistry.CreateNode != null) {
                CreateTagFormData data = new CreateTagFormData() {
                    TagType = type,
                    HasName = true,
                };
                data.RestrictedNames.AddRange(_container.TagNamesInUse);

                if (FormRegistry.CreateNode(data)) {
                    AddTag(data.TagNode, data.TagName);
                    return true;
                }
            }

            return false;
        }

        public override bool PasteNode ()
        {
            if (!CanPasteIntoNode)
                return false;

            NbtClipboardData clipboard = NbtClipboardController.CopyFromClipboard();
            if (clipboard == null || clipboard.Node == null)
                return false;

            string name = clipboard.Name;
            if (String.IsNullOrEmpty(name))
                name = "UNNAMED";

            AddTag(clipboard.Node, MakeUniqueName(name));
            return true;
        }

        public bool IsNamedContainer
        {
            get { return true; }
        }

        public bool IsOrderedContainer
        {
            get { return false; }
        }

        public INamedTagContainer NamedTagContainer
        {
            get { return _container; }
        }

        public IOrderedTagContainer OrderedTagContainer
        {
            get { return null; }
        }

        public int TagCount
        {
            get { return _container.TagCount; }
        }

        public bool DeleteTag (TagNode tag)
        {
            return _container.DeleteTag(tag);
        }

        public void AddTag (TagNode tag, string name)
        {
            if (_tree == null) {
                ExpandCore();
            }
            _container.AddTag(tag, name);
            IsDataModified = true;

            if (IsExpanded) {
                TagDataNode node = TagDataNode.CreateFromTag(tag);
                if (node != null)
                    Nodes.Add(node);
            }
        }

        private string MakeUniqueName (string name)
        {
            List<string> names = new List<string>(_container.TagNamesInUse);
            if (!names.Contains(name))
                return name;

            int index = 1;
            while (names.Contains(MakeCandidateName(name, index)))
                index++;

            return MakeCandidateName(name, index);
        }

        private string MakeCandidateName (string name, int index)
        {
            return name + " (Copy " + index + ")";
        }
    }
}
