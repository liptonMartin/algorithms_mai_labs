using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);
        
        if (Comparer.Compare(key, root.Key) <= 0) // key <= root.Key
        {
            var (t1, t2) = Split(root.Left, key);
            root.Left = t2;
            t2?.Parent = root;

            t1?.Parent = null;
            root?.Parent = null;
            
            return (t1, root);
        }

        else // key > root.Key
        {
            var (t1, t2) = Split(root.Right, key);
            root.Right = t1;
            t1?.Parent = root;

            t2?.Parent = null;
            root?.Parent = null;
            
            return (root, t2);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null || right == null)
        {
            // если оба null, то возращаем тоже null
            return (right == null ? left : right);
        }
        
        // слева всегда будем держать более приоритетное дерево
        if (right.Priority > left.Priority) (left, right) = (right, left);

        TreapNode<TKey, TValue> result = left;
        if (Comparer.Compare(left.Key, right.Key) > 0) // left.Key > right.Key
        {
            // то тогда правое поддерево полностью остается 
            // левое поддерево надо рекурсивно слить с оставшейся частью
            left.Left = Merge(left.Left, right);
            left.Left?.Parent = left;
        }
        else if (Comparer.Compare(left.Key, right.Key) < 0) // left.Key < right.Key} 
        {
            // то тогда левое поддерево полностью остается
            // правое поддерево надо рекурсивно слить с оставшейся частью
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
        }

        else
        {
            // надо отредактировать значение, но какое из значений брать ??? 
            // логика редактирования лежит в методе .Add()
            // запрещаю мержить два дерева с одинаковыми ключами
            throw new InvalidOperationException("Одинаковые ключи в двух деревьях");
        }

        return result;
    }
    

    public override void Add(TKey key, TValue value)
    {
        // если узел с таким ключом существует, то просто изменим его значение
        TreapNode<TKey, TValue>? existNode = FindNode(key);
        if (existNode != null)
        {
            existNode.Value = value;
            return;
        }
        
        // нужно смержить новый элемент и дерево,
        // но в дереве могут храниться как значения меньше добавляемого, так и больше
        // тогда два раза сплитанем и замержим
        
        TreapNode<TKey, TValue>? newNode = CreateNode(key, value);
        
        var (t1, t2) = Split(this.Root, key);
        TreapNode<TKey, TValue>? result = Merge(t1, newNode);
        result = Merge(result, t2);
        this.Root = result;
        this.Count++;
    }

    public override bool Remove(TKey key)
    {
        TreapNode<TKey, TValue>? node = FindNode(key);
        if (node == null) return false;
        
        // мержим двух детей и результат вставляем вместо удаляемого узла
        TreapNode<TKey, TValue>? newNode = Merge(node.Left, node.Right); 
        
        if (node.IsLeftChild) node.Parent?.Left = newNode;
        else if (node.IsRightChild) node.Parent?.Right = newNode;
        else this.Root = newNode;
        
        // null - если дерево было пустым
        newNode?.Parent = node.Parent;
        
        this.Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        TreapNode<TKey, TValue> newNode = new TreapNode<TKey, TValue>(key, value);
        return newNode;
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
        // не требуется
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
        // не требуется
    }

    // функция чисто для тестирования
    public TreapNode<TKey, TValue>? FindNodeByKey(TKey key)
    {
        TreapNode<TKey, TValue>? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }
    
    // функция чисто для тестирования
    public void Add(TKey key, TValue value, int priority)
    {
        // если узел с таким ключом существует, то просто изменим его значение
        TreapNode<TKey, TValue>? existNode = FindNode(key);
        if (existNode != null)
        {
            existNode.Value = value;
            return;
        }
        
        // нужно смержить новый элемент и дерево,
        // но в дереве могут храниться как значения меньше добавляемого, так и больше
        // тогда два раза сплитанем и замержим
        
        TreapNode<TKey, TValue>? newNode = new TreapNode<TKey, TValue>(key, value);
        newNode.Priority = priority;
        
        var (t1, t2) = Split(this.Root, key);
        TreapNode<TKey, TValue>? result = Merge(t1, newNode);
        result = Merge(result, t2);
        this.Root = result;
        this.Count++;
    }
    
}