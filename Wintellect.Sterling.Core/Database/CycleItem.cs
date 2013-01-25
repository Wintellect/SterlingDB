using System;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     Item to help prevent cycle cases
    /// </summary>
    public class CycleItem
    {
        public Type ClassType { get; set; }
        public object Instance { get; set; }
        public object Key { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CycleItem;
            return other != null &&
                   other.ClassType.Equals(ClassType) &&
                   other.Key.Equals(Key);
                
        }

        public override int GetHashCode()
        {
            return string.Format("{0}{1}", ClassType.AssemblyQualifiedName, Key).GetHashCode();
        }
    }
}