using System;
using TreeDataStructures.Implementations.AVL;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.Splay;
using TreeDataStructures.Implementations.Treap;

namespace TreeDataStructures;

class Program
{
    static void Main(string[] args)
    {
        // System.Console.WriteLine("Идет проверка Splay дерева");
        // TestSplayTree();
        //
        // System.Console.Write("\n\n\n\n");
        // System.Console.WriteLine("Идет проверка BST дерева");
        // TestBinarySearchTree();
        // 
        // System.Console.Write("\n\n\n\n");
        // System.Console.WriteLine("Идет проверка Treap дерева");
        // TestTreapTree();
        
        System.Console.Write("\n\n\n\n");
        System.Console.WriteLine("Идет проверка AVL дерева");
        
        TestAVLTree();
    }

    private static void TestSplayTree()
    {
        SplayTree<int, int> tree = new SplayTree<int, int>();
        
        tree.Add(5, 5);
        tree.Add(2, 2);
        tree.Add(3, 3);
        tree.Add(4, 4);
        tree.Add(10, 10);
        tree.Add(1, 1);
        
        System.Console.WriteLine(tree.Count);
        System.Console.WriteLine("Сейчас будет обход дерева в InOder порядке");
        
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }
        
        tree.TryGetValue(10, out int value);
        System.Console.WriteLine($"Элемент со значением 10 найден: {value}");
        System.Console.WriteLine("Дерево после поиска:");
        
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }

        tree.Remove(4);
        System.Console.WriteLine("Элемент с ключом 4 удален");
        System.Console.WriteLine("Дерево после удаления:");
        
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }

        tree.Remove(5);
        tree.Remove(1);
        tree.Remove(3);
        tree.Remove(10);

        System.Console.WriteLine($"Колво узлов {tree.Count}");
        System.Console.WriteLine("Дерево после удаления всех элементов, кроме 2:");
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }

        tree.Remove(2);
        System.Console.WriteLine($"Колво узлов {tree.Count}");
        System.Console.WriteLine("Дерево после удаления всех элементов:");
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }
    }

    private static void TestBinarySearchTree()
    {
        BinarySearchTree<int, int> tree = new BinarySearchTree<int, int>();
        
        tree.Add(5, 5);
        tree.Add(2, 2);
        tree.Add(3, 3);
        tree.Add(4, 4);
        tree.Add(10, 10);
        tree.Add(1, 1);
        
        System.Console.WriteLine(tree.Count);
        System.Console.WriteLine("Сейчас будет обход дерева в InOder порядке");
        
        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine(item.Key);
        }
        
        System.Console.WriteLine("Проверка индексатора:");
        tree[5] = 100000;
        tree.TryGetValue(5, out int value);
        System.Console.WriteLine($"Значение значения по ключу 5: {value}");
    }

    private static void TestTreapTree()
    {
        // Treap<int, int> tree = new Treap<int, int>();
        //
        // tree.Add(-7, -7, 100);
        // tree.Add(-10, -10, 50);
        // tree.Add(-8, -8, 20);
        // tree.Add(2, 2, 50);
        // tree.Add(6, 6, 30);
        // tree.Add(4, 4, 20);
        // tree.Add(8, 8, 10);
        // tree.Add(9, 9, 5);
        //
        // foreach (var item in tree.InOrder())
        // {
        //     for (int i = 0; i < item.Depth; ++i)
        //     {
        //         System.Console.Write("-");
        //     }
        //     System.Console.WriteLine($" ({item.Key})");
        // }
        //
        // bool result = tree.Remove(9);
        // result = tree.Remove(8);
        // result = tree.Remove(4);
        // result = tree.Remove(6);
        //
        // System.Console.Write("");
        
        Treap<int, int> tree = new Treap<int, int>();
        tree.Add(2, 2, 100);
        tree.Add(-8, -8, 50);
        tree.Add(-10, -10, 20);
        tree.Add(-7, -7, 10);
        
        tree.Add(8, 8, 50);
        tree.Add(9, 9, 40);
        tree.Add(6, 6, 40);
        tree.Add(4, 4, 30);

        foreach (var item in tree.InOrder())
        {
            for (int i = 0; i < item.Depth; ++i)
            {
                System.Console.Write("-");
            }
            System.Console.WriteLine($" ({item.Key})");
        }
        
        bool result = tree.Remove(9);
        System.Console.WriteLine(result == true);
        result = tree.Remove(8);
        System.Console.WriteLine(result == true);
        result = tree.Remove(4);
        System.Console.WriteLine(result == true);
        result = tree.Remove(6);
        System.Console.WriteLine(result == true);
        
        
        System.Console.Write("");
        
        
    }

    private static void TestAVLTree()
    {
        AvlTree<int, int> tree = new AvlTree<int, int>();
        
        tree.Add(5, 1);
        tree.Add(12, 2);
        tree.Add(7, 3);
        tree.Add(9, 4);
        tree.Add(10, 5);
        tree.Add(12, 2);
        tree.Add(13, 3);
        tree.Add(20, 3);
        tree.Add(30, 2);
        tree.Add(45, 57);
        tree.Add(24, 10);


        foreach (var item in tree.InOrder())
        {
            int key =  item.Key;
            bool result =  tree.Remove(key);
            if (result == false) throw new Exception();
        }
    }
}