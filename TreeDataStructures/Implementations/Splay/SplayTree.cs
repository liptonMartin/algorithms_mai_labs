using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.Treap;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void RemoveNode(BstNode<TKey, TValue> node)
    {
        Splay(node);
        
        base.RemoveNode(node);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        // ничего делать не надо
    }

    public override bool ContainsKey(TKey key)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node == null) return false;
        Splay(node);
        return true;
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.IsLeftChild || node.IsRightChild)
        {
            if (node.IsLeftChild) RotateRight(node);
            else if (node.IsRightChild) RotateLeft(node);
        }
    }
    
    
}
