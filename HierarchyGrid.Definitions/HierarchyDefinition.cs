using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace HierarchyGrid.Definitions
{
    public abstract class HierarchyDefinition : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        public HierarchyDefinition Parent { get; set; }

        public object Content { get; set; }

        public int Position { get; internal set; }

        /// <summary>
        /// Number of hierarchical elements from root. 0 based.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Position among sibling definitions.
        /// </summary>
        public int RelativePosition { get; set; }

        /// <summary>
        /// Relevent dimension of header (width for columns, height for rows)
        /// </summary>
        public double Size { get; set; }

        public bool Frozen { get; set; }

        private int _span = int.MinValue;

        /// <summary>
        /// Span in hierarchy, used for display purposes.
        /// </summary>
        /// <remarks>May be modified when scrolling! To get its 'real' span, use Count().</remarks>
        public int Span
        {
            get
            {
                if (_span == int.MinValue)
                    return this.Count();
                else
                    return _span;
            }
            set
            {
                _span = value;
            }
        }

        internal ImmutableList<HierarchyDefinition> Children
            => _children.ToImmutableList();

        protected LinkedList<HierarchyDefinition> _children
            = new LinkedList<HierarchyDefinition>();

        /// <summary>
        /// True if there is at least a child element, false otherwise.
        /// </summary>
        public bool HasChild => _children.Count > 0;

        /// <summary>
        /// Computes occupation of the current element in the hierarchy.
        /// </summary>
        /// <returns>1 if leaf or folded node, sum of children elements otherwise</returns>
        public int Count(bool ignoreState = false)
        {
            int cnt = 0;

            if (HasChild && (IsExpanded || ignoreState))
                foreach (var hdef in _children)
                    cnt += hdef.Count(ignoreState);
            else
                cnt = 1;

            return cnt;
        }

        /// <summary>
        /// Number of sublayers in the hierarchy from current element.
        /// </summary>
        /// <returns></returns>
        public int Depth(bool ignoreState = true)
        {
            int dpt = 1;

            if (HasChild && (IsExpanded || ignoreState))
                dpt += _children.Max(o => o.Depth(ignoreState));

            return dpt;
        }

        protected HierarchyDefinition()
        {
            Activator = new ViewModelActivator();

            this.WhenAnyValue(o => o.Parent)
                .WhereNotNull()
                .SubscribeSafe(p =>
                {
                    CanToggle = p.CanToggle;
                    Invalidate();
                });

            this.WhenAnyValue(o => o.CanToggle)
                .SubscribeSafe(can =>
                {
                    foreach (var child in _children)
                        child.CanToggle = can;
                });
        }

        /// <summary>
        /// Adds a properly set child element to the current hierarchy definition.
        /// </summary>
        /// <param name="child">Hierarchy definition to add</param>
        /// <returns>Added hierarchy definition</returns>
        public X Add<X>(X child) where X : HierarchyDefinition
        {
            child.Level = Level + 1;
            child.Parent = this;

            foreach (var lower in child._children)
                IncrementLevel(lower);

            child.RelativePosition = _children.Count;
            _children.AddLast(child);
            return child;
        }

        public X AddFirst<X>(X child) where X : HierarchyDefinition
        {
            child.Level = Level + 1;
            child.Parent = this;

            foreach (var lower in child._children)
                IncrementLevel(lower);

            child.RelativePosition = 0;

            foreach (var otherChild in _children)
                otherChild.RelativePosition++;
            _children.AddFirst(child);

            return child;
        }

        /// <summary>
        /// Increments level of all children if added in an existing node.
        /// </summary>
        protected void IncrementLevel(HierarchyDefinition definition)
        {
            definition.Level++;
            foreach (var child in definition._children)
                IncrementLevel(child);
        }

        /// <summary>
        /// Path through hierarchy from root to current element.
        /// </summary>
        public ImmutableList<HierarchyDefinition> Path
        {
            get
            {
                var path = new LinkedList<HierarchyDefinition>();
                path.AddLast(this);

                var hd = Parent;
                while (hd != null)
                {
                    path.AddFirst(hd);
                    hd = hd.Parent;
                }

                return path.ToImmutableList();
            }
        }

        public int RelativePositionFromRoot
        {
            get
            {
                if (_relativePositionFromRoot != -1)
                    return _relativePositionFromRoot;

                int pos = 0;

                foreach (var hd in Path)
                    pos += hd.RelativePosition;

                _relativePositionFromRoot = pos;
                return pos;
            }
        }

        public int RelativePositionFrom(HierarchyDefinition hierarchyDefinition)
            => new[] { hierarchyDefinition }.Leaves().IndexOf(this);

        public HierarchyDefinition Root
        {
            get
            {
                if (_root != null)
                    return _root;

                if (Parent == null)
                {
                    _root = this;
                    return this;
                }

                HierarchyDefinition hd = Parent, root = null;
                while (hd != null)
                {
                    root = hd;
                    hd = hd.Parent;
                }

                _root = root;
                return _root;
            }
        }

        #region Interaction

        private bool _isExpanded = true;

        public bool IsExpanded
        {
            get => CanToggle ? _isExpanded : true;
            set { if (CanToggle) _isExpanded = value; }
        }

        [Reactive] public bool CanToggle { get; set; } = true;

        /// <summary>
        /// Sets state to expanded for current element and all its children.
        /// </summary>
        internal void ExpandAll()
        {
            IsExpanded = true;
            foreach (var child in _children)
                child.ExpandAll();
        }

        internal void ExpandUpwards()
        {
            IsExpanded = true;

            HierarchyDefinition hd = Parent;
            hd?.ExpandUpwards();
        }

        /// <summary>
        /// Sets state to folded for current element and all its children.
        /// </summary>
        internal void FoldAll()
        {
            IsExpanded = false;
            foreach (var child in _children)
                child.FoldAll();
        }

        #endregion Interaction

        #region Cached info

        protected int _relativePositionFromRoot = -1;
        protected HierarchyDefinition _root;

        public void Invalidate()
        {
            _relativePositionFromRoot = -1;
            _root = null;

            foreach (var child in _children)
                child.Invalidate();
        }

        internal void UpdatePosition(ref int position)
        {
            Position = ++position;
            foreach (var child in _children)
                child.UpdatePosition(ref position);
        }

        #endregion Cached info

        public override string ToString()
            => string.Join('.', Path.Select(o => o.Content));
    }
}