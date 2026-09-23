using System;
using NBTExplorer.Utility;
using Substrate.Nbt;

namespace NBTExplorer.Model
{
    public enum TagSortMode
    {
        ContainersFirst,  // NBTExplorer default: Compounds first, Lists second, Scalars third, Arrays fourth
        Alphabetical,     // Pure Alphabetical A-Z by Tag Name
        TagType,          // By Tag Type enum value
        FileOrder         // Natural file insertion order
    }

    public class TagKey : IComparable<TagKey>
    {
        private static readonly NaturalComparer _comparer = new NaturalComparer();

        public static TagSortMode SortMode { get; set; } = TagSortMode.ContainersFirst;

        public TagKey (string name, TagType type, int insertIndex = 0)
        {
            Name = name;
            TagType = type;
            InsertIndex = insertIndex;
        }

        public string Name { get; set; }
        public TagType TagType { get; set; }
        public int InsertIndex { get; set; }

        public static int OrderForTag(TagType tagID)
        {
            switch (tagID)
            {
                case TagType.TAG_COMPOUND:
                    return 0; // Compounds FIRST
                case TagType.TAG_LIST:
                    return 1; // Lists SECOND
                case TagType.TAG_BYTE:
                case TagType.TAG_SHORT:
                case TagType.TAG_INT:
                case TagType.TAG_LONG:
                case TagType.TAG_FLOAT:
                case TagType.TAG_DOUBLE:
                case TagType.TAG_STRING:
                    return 2; // Scalars THIRD
                default:
                    return 3; // Byte Array, Int Array, Long Array FOURTH
            }
        }

        public int Compare (TagKey x, TagKey y)
        {
            if (SortMode == TagSortMode.FileOrder)
            {
                return x.InsertIndex.CompareTo(y.InsertIndex);
            }

            if (SortMode == TagSortMode.ContainersFirst)
            {
                int orderX = OrderForTag(x.TagType);
                int orderY = OrderForTag(y.TagType);
                int tagOrderDiff = orderX.CompareTo(orderY);
                if (tagOrderDiff != 0)
                    return tagOrderDiff;

                int nameDiff = _comparer.Compare(x.Name, y.Name);
                if (nameDiff != 0)
                    return nameDiff;

                return x.InsertIndex.CompareTo(y.InsertIndex);
            }

            if (SortMode == TagSortMode.Alphabetical)
            {
                int nameDiff = _comparer.Compare(x.Name, y.Name);
                if (nameDiff != 0)
                    return nameDiff;

                int orderX = OrderForTag(x.TagType);
                int orderY = OrderForTag(y.TagType);
                int tagOrderDiff = orderX.CompareTo(orderY);
                if (tagOrderDiff != 0)
                    return tagOrderDiff;

                return x.InsertIndex.CompareTo(y.InsertIndex);
            }

            if (SortMode == TagSortMode.TagType)
            {
                int typeDiff = (int)x.TagType - (int)y.TagType;
                if (typeDiff != 0)
                    return typeDiff;

                int nameDiff = _comparer.Compare(x.Name, y.Name);
                if (nameDiff != 0)
                    return nameDiff;

                return x.InsertIndex.CompareTo(y.InsertIndex);
            }

            return _comparer.Compare(x.Name, y.Name);
        }

        public int CompareTo (TagKey other)
        {
            return Compare(this, other);
        }
    }
}
