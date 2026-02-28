using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void BalancingAvlTree(ref AvlNode<TKey, TValue>? currentNode, ref int balance)
    {
        if (currentNode == null) return;
        
        int heightLeft = currentNode.Left?.Height ?? 0;
        int heightRight = currentNode.Right?.Height ?? 0;
        
        currentNode.Height = Math.Max(heightLeft, heightRight) + 1;
        
        balance = heightLeft - heightRight;
        
        if (balance == 2 || balance == -2)
        {
            AvlNode<TKey, TValue>? left = currentNode.Left;
            AvlNode<TKey, TValue>? right = currentNode.Right;

            int heightLeftLeft = left?.Left?.Height ?? 0;
            int heightLeftRight = left?.Right?.Height ?? 0;
            
            int heightRightLeft = right?.Left?.Height ?? 0;
            int heightRightRight = right?.Right?.Height ?? 0;
            
            int balanceLeftChild = heightLeftLeft - heightLeftRight;
            int balanceRightChild = heightRightLeft - heightRightRight;

            // малые повороты:
            if (balance == -2 && (balanceRightChild == -1 || balanceRightChild == 0))
            {
                // нужен малый левый поворот
                if (right == null) throw new InvalidOperationException();
                RotateLeft(right);

                // изменилась высота у currentNode и right
                currentNode.Height = Math.Max(currentNode.Left?.Height ?? 0, currentNode.Right?.Height ?? 0) + 1;
                right.Height = Math.Max(right.Left?.Height ?? 0, right.Right?.Height ?? 0) + 1;
                
                currentNode = right;
            }

            else if (balance == 2 && (balanceLeftChild == 1 || balanceLeftChild == 0))
            {
                // нужен малый правый поворот
                if (left == null) throw new InvalidOperationException();
                RotateRight(left);
                
                // изменилась высота у currentNode и left 
                currentNode.Height = Math.Max(currentNode.Left?.Height ?? 0, currentNode.Right?.Height ?? 0) + 1;
                left.Height = Math.Max(left.Left?.Height ?? 0, left.Right?.Height ?? 0) + 1;
                
                currentNode = left;
            }
            
            else if (balance == -2 && balanceRightChild == 1)
            {
                // нужен большой левый поворот
                if (right == null) throw new InvalidOperationException();
                
                AvlNode<TKey, TValue>? rightLeft = right.Left;
                if (rightLeft == null) throw new InvalidOperationException();

                RotateBigLeft(rightLeft);
                
                // изменилась высота у currentNode, right, rightLeft
                currentNode.Height = Math.Max(currentNode.Left?.Height ?? 0, currentNode.Right?.Height ?? 0) + 1;
                right.Height = Math.Max(right.Left?.Height ?? 0, right.Right?.Height ?? 0) + 1;
                rightLeft.Height = Math.Max(rightLeft.Left?.Height ?? 0, rightLeft.Right?.Height ?? 0) + 1;
                
                currentNode = rightLeft;
            }
            
            else if (balance == 2 && balanceLeftChild == -1)
            {
                // нужен большой правый поворот
                if (left == null) throw new InvalidOperationException();
                
                AvlNode<TKey, TValue>? leftRight = left.Right;
                if (leftRight == null) throw new InvalidOperationException();
                
                RotateBigRight(leftRight);
                
                // изменилась высота у currentNode, left, leftRight
                currentNode.Height = Math.Max(currentNode.Left?.Height ?? 0, currentNode.Right?.Height ?? 0) + 1;
                left.Height = Math.Max(left.Left?.Height ?? 0, left.Right?.Height ?? 0) + 1;
                leftRight.Height = Math.Max(leftRight.Left?.Height ?? 0, leftRight.Right?.Height ?? 0) + 1;
                
                currentNode = leftRight;
            }
            else
            {
                // нужна балансировка, но никакая не подходит, какие-то проблемы ранее при построении дерева
                throw new InvalidOperationException();
            }
        }
        else
        {
            currentNode = currentNode.Parent;
        }
            
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        int balance = 0;
        AvlNode<TKey, TValue>? currentNode = newNode.Parent;
        do
        {
            BalancingAvlTree(ref currentNode, ref balance);
        } while (balance != 0 && currentNode != null);
    }

    protected override void RemoveNode(AvlNode<TKey, TValue> node)
    {
        if (node.Right != null)
        {
            // если есть правый ребенок
            // нужно найти самого левого потомка в правом поддереве
            // заменить его на текущий узел
            
            AvlNode<TKey, TValue> mostLeftChildInRightSubTree = node.Right;
            while (mostLeftChildInRightSubTree.Left != null)
            {
                mostLeftChildInRightSubTree =  mostLeftChildInRightSubTree.Left;
            }
            
            // запомним родителя удаляемого узла, чтобы потом оттуда запустить балансировку
            // балансировка в методе OnNodeRemoved
            
            AvlNode<TKey, TValue>? parentDeletedNode = mostLeftChildInRightSubTree.Parent;
            
            Transplant(node, mostLeftChildInRightSubTree);
            // (у mostLeftChildRightSubTree нет детей, это лист, а вот у удаляемого узла были дети
            // теперь эти дети являются детьми mostLeftChildRightSubTree
            
            if (mostLeftChildInRightSubTree != node.Right)
                // если в правом поддереве есть хотя бы один левый потомок
            {
                parentDeletedNode?.Left = mostLeftChildInRightSubTree.Right;
                mostLeftChildInRightSubTree.Right?.Parent = parentDeletedNode;
                
                mostLeftChildInRightSubTree.Right = node.Right;
                node.Right?.Parent = mostLeftChildInRightSubTree;
                
            }
            else
            {
                // если в правом поддереве нет ни одного потомка,
                // то родитель удаляемого элемента (тот самый который мы переместили)
                // и будет mostLeftChildInRightTree
                parentDeletedNode = mostLeftChildInRightSubTree;
            }
            
            mostLeftChildInRightSubTree.Left = node.Left;
            node.Left?.Parent = mostLeftChildInRightSubTree;
            
            // также изменим node, мы же его переместили
            node.Left = null;
            node.Right = null;
            node.Parent = parentDeletedNode;
            
            OnNodeRemoved(parentDeletedNode, node);
        }
        
        else if (node.Left != null)
        {
            // если есть левый ребенок, но нет правого ребенка
            // нужно найти самого правого потомка в левом поддереве
            // заменить его на текущий узел
            
            AvlNode<TKey, TValue> mostRightChildInLeftSubTree = node.Left;
            while (mostRightChildInLeftSubTree.Right != null)
            {
                mostRightChildInLeftSubTree = mostRightChildInLeftSubTree.Right;
            }
            
            // запомним родителя удаляемого узла, чтобы потом оттуда запустить балансировку
            // балансировка в методе OnNodeRemoved
            
            AvlNode<TKey, TValue>? parentDeletedNode = mostRightChildInLeftSubTree.Parent;
            
            Transplant(node, mostRightChildInLeftSubTree);

            if (mostRightChildInLeftSubTree != node.Left)
                // в левом поддереве есть хотя бы один правый потомок
            {
                parentDeletedNode?.Right = mostRightChildInLeftSubTree.Left;
                mostRightChildInLeftSubTree.Left?.Parent = parentDeletedNode;
                
                mostRightChildInLeftSubTree.Left = node.Left;
                node.Left?.Parent = mostRightChildInLeftSubTree;
            }

            else
            {
                // если в левом поддереве нет ни правого одного потомка
                parentDeletedNode = mostRightChildInLeftSubTree;
            }
            
            mostRightChildInLeftSubTree.Right = node.Right;
            node.Right?.Parent = mostRightChildInLeftSubTree;
            
            // также изменим node, мы же его переместили
            node.Left = null;
            node.Right = null;
            node.Parent = parentDeletedNode;
            
            OnNodeRemoved(parentDeletedNode, node);
        }

        else
        {
            // нет детей, значит это лист
            // просто удалить ссылку на этот узел

            if (node.Parent != null && node.IsLeftChild) node.Parent.Left = null;
            else if (node.Parent != null && node.IsRightChild) node.Parent.Right = null;
            else Root = null; //  нет родителя - значит узел
            
            OnNodeRemoved(node.Parent, node);
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        // нужно запустить балансировку от родителя удаляемого узла

        int balance = 0;
        while (balance != 1 && balance != -1 && parent != null)
        {
            BalancingAvlTree(ref parent, ref balance);
        }
    }
}