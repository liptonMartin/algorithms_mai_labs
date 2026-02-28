using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            foreach (var node in InOrder())
            {
                keys.Add(node.Key);
            }

            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            foreach (var node in InOrder())
            {
                values.Add(node.Value);
            }

            return values;
        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);
        
        TNode? currentNode = this.Root; // начинаем перебирать с корня
        TNode? leftOrRightChild = this.Root; // левый или правый потомок
        
        // в этом подходе учтено, если дерево пустое
        
        // вставляем в лист
        // или в узел, если ключ совпадает
        while (leftOrRightChild != null)
        {
            // если равно, то нашли место
            // если меньше, то продолдаем поиск в левом поддереве
            // иначе продолджаем поиск, куда вставить узел, в правом поддереве
            currentNode = leftOrRightChild;

            int cmp = Comparer.Compare(newNode.Key, leftOrRightChild.Key);
            if (cmp == 0) break;
            else if (cmp < 0) leftOrRightChild = leftOrRightChild.Left;
            else leftOrRightChild = leftOrRightChild.Right;
        }
        
        // после этого цикла currentNode - родитель нового узла, или, если такой ключ уже был, то сам узел
        if (currentNode != null && Comparer.Compare(newNode.Key, currentNode.Key) == 0)
        {
            currentNode.Value = value;
        }

        else
        {
            // определяем родителя у нового узла
            newNode.Parent = currentNode;
            // у родителя нового узла определяем сына (новый узел)
            if (currentNode != null && Comparer.Compare(newNode.Key, currentNode.Key) <= 0) currentNode.Left = newNode;
            else if (currentNode != null && Comparer.Compare(newNode.Key, currentNode.Key) > 0) currentNode.Right = newNode;
        
            if (currentNode == null) this.Root = newNode;
            
            this.Count++;
        }
        
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        // удалили узел node, заменив его node.Right
        if (node.Right != null)
        {
            Transplant(node, node.Right);
            
            // левый потомок node остался висеть
            // нужно его прикрепить к самому левому потомку правого поддерева слева
        
            // ищем самого левого потомка левого поддерева
            TNode? mostLeftNode = node.Right;
            while (mostLeftNode?.Left != null) mostLeftNode = mostLeftNode.Left;
        
            // в случае, если mostLeftNode == null, то тогда дерево состояло из одного элемента
            mostLeftNode?.Left = node.Left;
            node.Left?.Parent = mostLeftNode;

            this.OnNodeRemoved(node.Parent, node.Right);
        }
        else if (node.Left != null)
        {
            // если нет правого поддерева
            Transplant(node, node.Left);
            
            // правый потомок node остался висеть
            // нужно его прикрепить к самому правому потомку левого поддерева справа
        
            // ищем самого правый потомок правого поддерева
            TNode? mostRightNode = node.Left;
            while (mostRightNode?.Right != null) mostRightNode = mostRightNode.Right;
        
            // в случае, если mostRightNode == null, то тогда дерево состояло из одного элемента
            mostRightNode?.Right = node.Right;
            node.Right?.Parent = mostRightNode;

            this.OnNodeRemoved(node.Parent, node.Left);
        }
        else 
        {
            // нет детей
            // просто удалить ссылку
            if (node.IsLeftChild) node.Parent?.Left = null;
            else if (node.IsRightChild) node.Parent?.Right = null;
            
            // если был один узел (нет родителя - корень, нет детей - лист)
            if (node.Parent == null) Root = null;
            
            this.OnNodeRemoved(node.Parent, null);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        // если корень, то ничего не делаем
        if (x.Parent == null) return;

        TNode p = x.Parent;
        
        // меняем у родителя родителя ссылку на ребенка 
        if (p.IsLeftChild) p.Parent?.Left = x;
        if (p.IsRightChild) p.Parent?.Right = x;
        
        // меняем у основного узла ссылку на родителя
        x.Parent = p.Parent;
        
        // меняем левого потомка с правым потомком родителя
        TNode? temp = x.Left;
        
        // основная логика
        x.Left = p;
        p.Parent = x;
        
        p.Right = temp;
        temp?.Parent = p;
        
        // если мы свапнули с корнем, то корень изменился
        if (p == this.Root) this.Root = x;
    }

    protected void RotateRight(TNode y)
    {
        // если корень, то ничего не делаем
        if (y.Parent == null) return;

        TNode p = y.Parent;
        
        // меняем у родителя родителя ссылку на ребенка 
        if (p.IsLeftChild) p.Parent?.Left = y;
        if (p.IsRightChild) p.Parent?.Right = y;
        
        // меняем у основного узла ссылку на родителя
        y.Parent = p.Parent;
        
        // меняем правого потомка с левым потомком родителя
        TNode? temp = y.Right;
        
        // основная логика
        y.Right = p;
        p.Parent = y;
        
        p.Left = temp;
        temp?.Parent = p;
        
        // если мы свапнули с корнем, то корень изменился
        if (p == this.Root) this.Root = y;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        this.RotateRight(x);
        this.RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        this.RotateLeft(y);
        this.RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {   
        if (x.Parent != null) this.RotateLeft(x.Parent);
        this.RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y.Parent != null) this.RotateRight(y.Parent);
        this.RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => 
        new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => 
        new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => 
        new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy; // or make it template parameter?

        private readonly TNode? _root; 
        // TNode-узел, int-глубина, int-состояние (для InOrder и PostOrder)
        private readonly Stack<(TNode, int, int)> _stack; 
        private TreeEntry<TKey, TValue>? _current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            this._root = root;
            this._strategy = strategy;
            this._current = null;

            this._stack = new Stack<(TNode, int, int)>();
            if (this._root != null)
            {
                // начинаем всегда с корня, глубина - 0, состояние - 0 
                _stack.Push((this._root, 0, 0));
            }
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_current != null) return _current.Value;
                throw new InvalidOperationException();
            }
        }
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_stack.Count != 0)
            {
                var (node, depth, state) = _stack.Pop();
                
                switch (_strategy)
                {
                    case TraversalStrategy.PreOrder:
                    {
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                        // обрабатываем сначала правое поддерево (потому что стек)
                        if (node.Right != null) _stack.Push((node.Right, depth + 1, state));
                        // затем обрабатываем левое поддерево
                        if (node.Left != null) _stack.Push((node.Left, depth + 1, state));

                        return true;
                    }

                    case TraversalStrategy.PostOrder:
                    {
                        // state == 1 - обработали узел, можно выдавать
                        // state == 0 - еще не обработанные дети
                        while (state != 1)
                        {
                            // если еще не обработали корень,
                            // то нужно добавить его детей, а затем этот же узел
                            // из-за стека, нужно сделать все в обратном порядке
                            // сначала правого ребенка, потом левых
                            
                            _stack.Push((node, depth, 1));
                            
                            if (node.Right != null) _stack.Push((node.Right, depth + 1, 0));
                            if (node.Left != null) _stack.Push((node.Left, depth + 1, 0));

                            (node, depth, state) = _stack.Pop();
                        }
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                        
                        return true;
                    }

                    case TraversalStrategy.InOrder:
                    {
                        // state == 1 - обработали узел, можно выдавать
                        // state == 0 - еще не обработанные дети
                        while (state != 1)
                        {
                            
                            if (node.Right != null) _stack.Push((node.Right, depth + 1, 0));

                            _stack.Push((node, depth, 1));

                            if (node.Left != null) _stack.Push((node.Left, depth + 1, 0));
                            (node, depth, state) = _stack.Pop();
                        } 
                        
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                        
                        return true;
                    }

                    case TraversalStrategy.PreOrderReverse:
                    {
                        while (state != 1)
                        {
                            _stack.Push((node, depth, 1));
                            
                            if (node.Left != null) _stack.Push((node.Left, depth + 1, 0));
                            if (node.Right != null) _stack.Push((node.Right, depth + 1, 0));
                            
                            (node, depth, state) = _stack.Pop();
                        }
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);

                        return true;
                    }

                    case TraversalStrategy.PostOrderReverse:
                    {
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                        
                        if (node.Left != null) _stack.Push((node.Left, depth + 1, state));
                        if (node.Right != null) _stack.Push((node.Right, depth + 1, state));

                        return true;
                    }

                    case TraversalStrategy.InOrderReverse:
                    {
                        // state == 1 - обработали узел, можно выдавать
                        // state == 0 - еще не обработанные дети
                        while (state != 1)
                        {
                            // сначала добавляем всех левых детей (из-за структуры стека)
                            if (node.Left != null) _stack.Push((node.Left, depth + 1, 0));
                            
                            // обрабатываем корень
                            _stack.Push((node, depth, 1));

                            // обрабатываем правые узлы
                            if (node.Right != null) _stack.Push((node.Right, depth + 1, 0));
                            (node, depth, state) = _stack.Pop();
                        } 
                        
                        _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                        
                        return true;
                    }
                }
            }

            return false;
        }
        
        public void Reset()
        {
            this._current = null;

            this._stack.Clear();
            if (this._root != null)
            {
                // начинаем всегда с корня, глубина - 0, состояние - 0 
                _stack.Push((this._root, 0, 0));
            }
        }

        
        public void Dispose()
        {
            this._stack.Clear();
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(entry => new KeyValuePair<TKey, TValue>(entry.Key, entry.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException();
        if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
        if (arrayIndex + Count > array.Length) throw new ArgumentOutOfRangeException();
        
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}