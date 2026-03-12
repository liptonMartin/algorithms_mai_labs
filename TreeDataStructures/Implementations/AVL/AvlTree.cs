using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void BalancingAvlTree(AvlNode<TKey, TValue> currentNode)
    {
        int balance = GetBalanceNode(currentNode);
    
        // левое поддерево больше
        if (balance > 1)
        {
            if (GetBalanceNode(currentNode.Left!) < 0)
            {
                RotateLeft(currentNode.Left!.Right!);
            }
            RotateRight(currentNode.Left!);
        }

        // правое поддерево больше
        else if (balance < -1)
        {
            if (GetBalanceNode(currentNode.Right!) > 0)
            {
                RotateRight(currentNode.Right!.Left!);
            }
            RotateLeft(currentNode.Right!);
        }
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        UpdateHeightNode(newNode);
        AvlNode<TKey, TValue>? currentNode = newNode.Parent;
        if (currentNode == null) return;
        
        while (currentNode != null)
        {
            BalancingAvlTree(currentNode);
            UpdateHeightNode(currentNode);
            int balance = GetBalanceNode(currentNode);
            currentNode = currentNode.Parent;
        }
    }

    protected override void RemoveNode(AvlNode<TKey, TValue> node)
    {
        // у узла нет детей
        if (node.Left == null && node.Right == null)
        {
            if (node.IsLeftChild) node.Parent?.Left = null;
            else if (node.IsRightChild) node.Parent?.Right = null;
            else Root = null; // удалили корень
            
            OnNodeRemoved(node.Parent, node);
            
            node.Parent = null;
        }
        
        // у узла есть только правый ребенок
        else if (node.Left == null && node.Right != null)
        {
            Transplant(node, node.Right);
            
            OnNodeRemoved(node.Parent, node.Right);
            
            node.Right = null;
            node.Parent = null;
        }
        
        // у узла есть только левый ребенок
        else if (node.Left != null && node.Right == null)
        {
            Transplant(node, node.Left);

            OnNodeRemoved(node.Parent, node.Left);
            
            node.Left = null;
            node.Parent = null;

        }

        // два ребенка у узла
        else if (node.Left != null && node.Right != null)
        {
            AvlNode<TKey, TValue> mostLeftNodeInRightSubtree = FindMostLeftNodeInRightSubtree(node)!;
            
            node.Key = mostLeftNodeInRightSubtree.Key;
            node.Value = mostLeftNodeInRightSubtree.Value;

            if (mostLeftNodeInRightSubtree.IsLeftChild)
                mostLeftNodeInRightSubtree.Parent!.Left = mostLeftNodeInRightSubtree.Right;
            else mostLeftNodeInRightSubtree.Parent!.Right = mostLeftNodeInRightSubtree.Right;

            mostLeftNodeInRightSubtree.Right?.Parent = mostLeftNodeInRightSubtree.Parent;
            
            OnNodeRemoved(mostLeftNodeInRightSubtree.Parent, node.Right);
            
            mostLeftNodeInRightSubtree.Parent = null;
            mostLeftNodeInRightSubtree.Right = null;
        }

        else
        {
            throw new InvalidOperationException("Internal Error!");
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        // нужно запустить балансировку от родителя удаляемого узла
        if (parent == null) return;
    
        var current = parent;
        while (current != null)
        {
            int oldHeight = current.Height;
            UpdateHeightNode(current);
        
            int balance = GetBalanceNode(current);
        
            if (Math.Abs(balance) >= 2)
            {
                BalancingAvlTree(current);
            }
            
            current = current.Parent;
        }
    }

    protected override void RotateLeft(AvlNode<TKey, TValue> x)
    {
        base.RotateLeft(x);
        
        if (x.Left != null) UpdateHeightNode(x.Left);
        UpdateHeightNode(x);
    }

    protected override void RotateRight(AvlNode<TKey, TValue> y)
    {
        base.RotateRight(y);
        
        if (y.Right != null) UpdateHeightNode(y.Right);
        UpdateHeightNode(y);
    }
    
    private AvlNode<TKey, TValue>? FindMostLeftNodeInRightSubtree(AvlNode<TKey, TValue> node)
    {
        AvlNode<TKey, TValue>? currentNode = node.Right;
        while (currentNode?.Left != null) currentNode = currentNode.Left;
        return currentNode;
    }

    private void UpdateHeightNode(AvlNode<TKey, TValue> node)
    {
        node.Height = Math.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0) + 1;
    }

    private int GetBalanceNode(AvlNode<TKey, TValue> node)
    {
        int leftHeight = node.Left?.Height ?? 0;
        int rightHeight = node.Right?.Height ?? 0;
        return leftHeight - rightHeight;
    }
}