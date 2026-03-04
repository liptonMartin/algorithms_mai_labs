using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (newNode == Root)
        {
            newNode.Color = RbColor.Black;
            return;
        }
        RbNode<TKey, TValue> currentNode = newNode;
        while (currentNode.Parent != null && currentNode.Parent.Color == RbColor.Red)
        {
            RbNode<TKey, TValue> father = currentNode.Parent;
            RbNode<TKey, TValue>? grandfather = father.Parent;
            if (grandfather == null) throw new InvalidOperationException("При добавлении узла произошла ошибка (красный корень?)");

            if (father.IsLeftChild)
            {
                if (grandfather.Right != null && grandfather.Right.Color == RbColor.Red)
                {
                    // если дядя тоже красный
                    grandfather.Right.Color = RbColor.Black;
                    father.Color = RbColor.Black;
                    grandfather.Color = RbColor.Red;
                    currentNode = grandfather;
                }
                else
                // дядя черный или его нет
                {
                    if (currentNode.IsRightChild)
                    {
                        RotateLeft(currentNode);
                        // Теперь father это левый потомок currentNode.
                        // Теперь смотрим относительного него
                        (currentNode, father) = (father, currentNode);
                    }
                    
                    grandfather.Color = RbColor.Red;
                    father.Color =  RbColor.Black;
                    
                    RotateRight(father);
                    
                    // так как теперь отец стал "корнем"
                    currentNode = father;
                }
            }
            else // father.IsRightChild
            {
                if (grandfather.Left != null && grandfather.Left.Color == RbColor.Red)
                {
                    // если дядя тоже красный
                    grandfather.Left.Color = RbColor.Black;
                    father.Color = RbColor.Black;
                    grandfather.Color = RbColor.Red;
                    currentNode = grandfather;
                }
                else
                {
                    // если дяди нет, или он черный
                    if (currentNode.IsLeftChild)
                    {
                        RotateRight(currentNode);
                        (currentNode, father) = (father, currentNode);
                    }
                    
                    grandfather.Color = RbColor.Red;
                    father.Color = RbColor.Black;
                    
                    RotateLeft(father);
                    
                    // так как теперь отец стал "корнем"
                    currentNode = father;
                }
            }
        }
        
        Root?.Color = RbColor.Black;
    }

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        // у узла нет детей
        if (node.Left == null && node.Right == null)
        {
            OnNodeRemoved(node.Parent, node);
            
            if (node.IsLeftChild) node.Parent?.Left = null;
            else if (node.IsRightChild) node.Parent?.Right = null;
            else Root = null; // удалили корень
            
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
            RbNode<TKey, TValue> mostLeftNodeInRightSubtree = node.Right;

            while (mostLeftNodeInRightSubtree.Left != null)
            {
                mostLeftNodeInRightSubtree = mostLeftNodeInRightSubtree.Left;
            }

            if (mostLeftNodeInRightSubtree.Parent == null)
                throw new InvalidOperationException("Неправильная структура дерева!");
            
            RbNode<TKey, TValue> parentDeleteNode =  mostLeftNodeInRightSubtree.Parent;
            RbNode<TKey, TValue>? childMostLeftNodeInRightSubtree = mostLeftNodeInRightSubtree.Right;
            if (mostLeftNodeInRightSubtree == node.Right)
            {
                parentDeleteNode = mostLeftNodeInRightSubtree;
            }
            else // mostLeftNodeInRightSubtree != node.Right
            {
                parentDeleteNode.Left = childMostLeftNodeInRightSubtree;
            }

            
            Transplant(node, mostLeftNodeInRightSubtree);
            
            mostLeftNodeInRightSubtree.Left = node.Left;
            node.Left?.Parent = mostLeftNodeInRightSubtree;

            
            if (mostLeftNodeInRightSubtree == node.Right)
            {
                mostLeftNodeInRightSubtree.Right = node.Right.Right;
            }
            else
            {
                mostLeftNodeInRightSubtree.Right = node.Right;
            }
            mostLeftNodeInRightSubtree.Right?.Parent = mostLeftNodeInRightSubtree;

            if (childMostLeftNodeInRightSubtree == null)
            {
                childMostLeftNodeInRightSubtree = node;
                // TODO: не нужно ли написать node.Color = RbColor.Black
                childMostLeftNodeInRightSubtree.Parent = parentDeleteNode;

                if (parentDeleteNode == mostLeftNodeInRightSubtree)
                {
                    parentDeleteNode.Right = childMostLeftNodeInRightSubtree;
                }
                else
                {
                    parentDeleteNode.Left = childMostLeftNodeInRightSubtree;
                }
            }
            
            OnNodeRemoved(parentDeleteNode, childMostLeftNodeInRightSubtree);
            if (childMostLeftNodeInRightSubtree == node)
            {
                childMostLeftNodeInRightSubtree.Right = null;
                if (childMostLeftNodeInRightSubtree.Parent == null)
                    throw new InvalidOperationException("Ошибка в структуре дерева!");
                else if (childMostLeftNodeInRightSubtree.IsLeftChild) childMostLeftNodeInRightSubtree.Parent.Left = null;
                else if (childMostLeftNodeInRightSubtree.IsRightChild) childMostLeftNodeInRightSubtree.Parent.Right = null;
                childMostLeftNodeInRightSubtree.Parent = null;
            }

            node.Right = null;
            node.Left = null;
            node.Parent = null;
        }

        else
        {
            throw new InvalidOperationException("Internal Error!");
        }
        
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (child == null) return; // балансировка не нужна
        
        while (child.Color == RbColor.Black && child != Root)
        {
            if (child.Parent == null) throw new InvalidOperationException("Ошибка в структуре дерева!");
            
            if (child.IsLeftChild)
            {
                RbNode<TKey, TValue> father = child.Parent;
                RbNode<TKey, TValue>? brother = father.Right;

                if (brother != null && brother.Color == RbColor.Red)
                    // случай 1: есть красный брат, надо повернуть дерево
                {
                    father.Color = RbColor.Red;
                    brother.Color = RbColor.Black;
                    RotateLeft(brother);

                    // после поворота изменился брат
                    brother = father.Right;
                }

                
                if (brother == null)
                {
                    father.Color = RbColor.Black;
                    child = father;
                    continue;
                }
                
                // случай 2: брат черный
                // случай 2.1: у брата оба черных ребенка (в том числе если их нет, ведь лист черного цвета)
                RbNode<TKey, TValue>? leftChildBrother = brother.Left; // левый ребенок брата
                RbNode<TKey, TValue>? rightChildBrother = brother.Right; // правый ребенок брата

                if ((leftChildBrother == null || leftChildBrother.Color == RbColor.Black) &&
                    (rightChildBrother == null || rightChildBrother.Color == RbColor.Black))
                {
                    brother.Color = RbColor.Red;
                    // father.Color = RbColor.Black;

                    child = father;
                }

                else
                // у брата есть хотя бы один не черный ребенок 
                {
                    // случай 2.2: у брата правый ребенок черный, а левый красный
                    if (leftChildBrother != null && leftChildBrother.Color == RbColor.Red &&
                        (rightChildBrother == null || rightChildBrother.Color == RbColor.Black))
                    {
                        brother.Color = RbColor.Red;
                        leftChildBrother.Color = RbColor.Black;

                        RotateRight(leftChildBrother);
                        
                        brother = leftChildBrother;
                        rightChildBrother =  brother.Right;
                        leftChildBrother = brother.Left;
                    }
                    
                    // случай 2.3: у брата правый ребенок красный, а левый черный
                    if (rightChildBrother == null)
                        throw new InvalidOperationException("Неправильная структура дерева!");

                    brother.Color = father.Color;
                    father.Color = RbColor.Black;
                    rightChildBrother.Color = RbColor.Black;
                    
                    RotateLeft(brother);

                    child = Root;
                }
            }
            else // child.IsRightChild
            {
                RbNode<TKey, TValue> father = child.Parent;
                RbNode<TKey, TValue>? brother = father.Left;
                
                if (brother != null && brother.Color == RbColor.Red)
                    // случай 1: есть красный брат, надо повернуть дерево
                {
                    father.Color = RbColor.Red;
                    brother.Color = RbColor.Black;
                    RotateRight(brother);

                    // после поворота изменился брат
                    brother = father.Left;
                }
                
                if (brother == null)
                {
                    child = father;
                    continue;
                }
                
                // случай 2: брат черный
                // случай 2.1: у брата оба черных ребенка (в том числе если их нет, ведь лист черного цвета)
                RbNode<TKey, TValue>? leftChildBrother = brother.Left; // левый ребенок брата
                RbNode<TKey, TValue>? rightChildBrother = brother.Right; // правый ребенок брата

                if ((leftChildBrother == null || leftChildBrother.Color == RbColor.Black) &&
                    (rightChildBrother == null || rightChildBrother.Color == RbColor.Black))
                {
                    brother.Color = RbColor.Red;
                    // father.Color = RbColor.Black;

                    child = father;
                }
                
                else
                    // у брата есть хотя бы один не черный ребенок 
                {
                    // случай 2.2: у брата левый ребенок черный, а правый красный
                    if (rightChildBrother != null && rightChildBrother.Color == RbColor.Red &&
                        (leftChildBrother == null || leftChildBrother.Color == RbColor.Black))
                    {
                        brother.Color = RbColor.Red;
                        rightChildBrother.Color = RbColor.Black;

                        RotateLeft(rightChildBrother);
                        
                        brother = rightChildBrother;
                        leftChildBrother =  brother.Left;
                        rightChildBrother = brother.Right;
                    }
                    
                    // случай 2.3: у брата левый ребенок красный, а правый черный
                    if (leftChildBrother == null)
                        throw new InvalidOperationException("Неправильная структура дерева!");

                    brother.Color = father.Color;
                    father.Color = RbColor.Black;
                    leftChildBrother.Color = RbColor.Black;
                    
                    RotateRight(brother);

                    child = Root;
                }
                
            }
        }
        
        child.Color = RbColor.Black;
        Root?.Color = RbColor.Black;
    }
    
    public RbNode<TKey, TValue>? PublicFindNode(TKey key)
    {
        RbNode<TKey, TValue>? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }
}