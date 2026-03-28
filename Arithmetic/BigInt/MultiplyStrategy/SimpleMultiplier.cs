using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    private const int CountDigit = sizeof(uint) * 8; // колво битов в цифре
    private const int CountHalfDigit = (sizeof(uint) / 2) * 8; // половина колва битов в цифре 
    private const uint MaskForRightHalf = (1 << CountHalfDigit) - 1; // маска для правой половинки (единицы в правой части) 
    private const uint MaskForLeftHalf = MaskForRightHalf << CountHalfDigit; // маска для левой половинки (единицы в левой части)
    
    private const int ShiftLeftHalf = (sizeof(uint) / 2) * 8; // насколько надо сдвигать левую половинку
    
    
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if ((!a.IsNegative && !b.IsNegative) || (a.IsNegative && b.IsNegative))
        {
            // a * b || -a * -b
            return HandleMultiplyOperation(a, b);
        }
        // -a * b || a * (-b)
        return -HandleMultiplyOperation(a, b);
    }

    private static BetterBigInteger HandleMultiplyOperation(BetterBigInteger a, BetterBigInteger b)
    {
        BetterBigInteger result = new BetterBigInteger([0]);
        
        var digitsA = a.GetDigits();
        var digitsB = b.GetDigits();
        for (int j = 0; j < digitsB.Length; j++)
        {
            for (int i = 0; i < digitsA.Length; i++)
            {
                BetterBigInteger resultMultiplyDigit = MultiplyDigit(digitsA[i], digitsB[j]);
                int shift = i * CountDigit +  j * CountDigit;
                resultMultiplyDigit <<= shift;
                result += resultMultiplyDigit;
            }
        }
        
        result.RemoveForwardZeros();
        return result;
    }


    private static BetterBigInteger MultiplyDigit(uint firstDigit, uint secondDigit)
    {
        /* умножает две цифры числа
         * 
         * получается нужно 4 умножения:
         * - правая половинка на правую
         * - правая на левую
         * - левая на правую
         * - левая на левую
         */

        uint rightHalfFirstDigit = firstDigit & MaskForRightHalf;
        uint rightHalfSecondDigit = secondDigit & MaskForRightHalf;
        
        uint leftHalfFirstDigit = (firstDigit & MaskForLeftHalf) >> CountHalfDigit;
        uint leftHalfSecondDigit = (secondDigit & MaskForLeftHalf) >> CountHalfDigit;
        
        BetterBigInteger rightRight = MultiplyHalfDigit(rightHalfFirstDigit, rightHalfSecondDigit);
        BetterBigInteger leftRight = MultiplyHalfDigit(leftHalfFirstDigit, rightHalfSecondDigit) << ShiftLeftHalf;
        BetterBigInteger rightLeft =  MultiplyHalfDigit(rightHalfFirstDigit, leftHalfSecondDigit) << ShiftLeftHalf;
        BetterBigInteger leftLeft = MultiplyHalfDigit(leftHalfFirstDigit, leftHalfSecondDigit) << (ShiftLeftHalf + ShiftLeftHalf);

        return rightRight + leftRight + rightLeft + leftLeft;
    }

    private static BetterBigInteger MultiplyHalfDigit(uint firstHalfDigit, uint secondHalfDigit)
    {
        /* умножает две половинки
         *
         * firstHalfDigit:: половинка, сдвинутая максимальная вправо
         * secondHalfDigit:: половинка, сдвинутая максимальная вправо
         */
        return new BetterBigInteger([firstHalfDigit * secondHalfDigit]);
    }
}