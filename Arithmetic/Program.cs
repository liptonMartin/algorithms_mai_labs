using System.Numerics;
using Arithmetic.BigInt;

namespace Arithmetic;
class Program
{
    private static void Main()
    {
        TestPlusAndMinus();
        
        TestSimpleMultiplication();

    }

    private static void TestPlusAndMinus()
    {
        Random rnd = new();
        for (int i = 0; i < 1000; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(1, 20));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(1, 20));
            
            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);
            
            BigInteger expA = BigInteger.Parse(s1);
            BigInteger expB = BigInteger.Parse(s2);
            
            BetterBigInteger myResult = myA - myB;
            BigInteger expResult = expA - expB;
            

            if (myResult.ToString() != expResult.ToString())
            {
                Console.WriteLine($"НЕПРАВИЛЬНО РАЗНОСТЬ!!!!!!!!!\tmyResult: {myResult}, expResult: {expResult}, myA: {myA}, myB: {myB}");   
            }
            
            myResult = myA + myB;
            expResult = expA + expB;

            if (myResult.ToString() != expResult.ToString())
            {
                Console.WriteLine($"НЕПРАВИЛЬНО СУММА!!!!!!!!!\tmyResult: {myResult}, expResult: {expResult}, myA: {myA}, myB: {myB}");   
            }
            
        }
    }

    private static void TestSimpleMultiplication()
    {
        Random rnd = new();
        for (int i = 0; i < 1000; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(1, 20));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(1, 20));

            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);

            BigInteger expA = BigInteger.Parse(s1);
            BigInteger expB = BigInteger.Parse(s2);
            
            BetterBigInteger myResult = myA * myB;
            BigInteger expResult = expA * expB;

            if (myResult.ToString() != expResult.ToString())
            {
                Console.WriteLine(
                    $"НЕПРАВИЛЬНО УМНОЖЕНИЕ!!!!!!!!!\tmyResult: {myResult}, expResult: {expResult}, myA: {myA}, myB: {myB}");
            }
        }
    }
    
    private static string GenerateLargeRandomString(Random rnd, int length)
    {
        char[] digits = new char[length];
        digits[0] = (char)rnd.Next('1', '9' + 1);
        for (int i = 1; i < length; i++)
        {
            digits[i] = (char)rnd.Next('0', '9' + 1);
        }
        
        return (rnd.Next(2) == 0 ? "-" : "") + new string(digits);
    }
}