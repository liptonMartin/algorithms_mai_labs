using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    const int CountBitsPerDigit = sizeof(uint) * 8; // количество битов в одной цифре
    private const int CountHalfDigit = CountBitsPerDigit / 2; // половина колва битов в цифре 
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

    private BetterBigInteger HandleMultiplyOperation(BetterBigInteger a, BetterBigInteger b)
    {
        /* рекурсивная функция умножения двух чисел */
        if (a.GetDigits().Length == 1 && b.GetDigits().Length == 1)
        {
            // база рекурсии
            SimpleMultiplier multiplier = new SimpleMultiplier();
            return multiplier.Multiply(a, b);
        }
        
        /* делим два числа на половинки, умножаем их, рекурсивно запускаясь от них */
        var countDigitsForDivide = FindCountDigitsForDivide(a, b);
        
        a = AddLeadingZeros(a, countDigitsForDivide);
        b = AddLeadingZeros(b, countDigitsForDivide);
        
        var lengthHalf = countDigitsForDivide / 2;
        var countBitsHalf = lengthHalf * CountBitsPerDigit;

        var bigOne = new BetterBigInteger([1]);
        var maskRightHalf = (bigOne << countBitsHalf) - bigOne; // единицы справа
        var maskLeftHalf = maskRightHalf << countBitsHalf; // единицы слева
        
        var rightHalfA = a & maskRightHalf;
        var leftHalfA = (a &  maskLeftHalf) >> countBitsHalf;
        
        var rightHalfB = b & maskRightHalf;
        var leftHalfB = (b & maskLeftHalf) >> countBitsHalf;

        var multiplicationRightHalfs = HandleMultiplyOperation(rightHalfA, rightHalfB); // A_0 * A_1
        var multiplicationLeftHalfs = HandleMultiplyOperation(leftHalfA, leftHalfB); // B_0 * B_1
        
        var sumHalfsA = rightHalfA + leftHalfA; // A_0 + A_1
        var sumHalfsB = rightHalfB + leftHalfB; // B_0 + B_1
        
        var multiplicationSumHalfs = HandleMultiplyOperation(sumHalfsA, sumHalfsB); // (A_0 + A_1) * (B_0 * B_1) 

        // A_0 * B_1 + A_1 * B_0 = (A_0 + A_1) * (B_0 * B_1) - A_0 * A_1 - B_0 * B_1
        var multiplicationDiagonal = multiplicationSumHalfs - multiplicationRightHalfs - multiplicationLeftHalfs;

        var shiftHalfs = lengthHalf * CountBitsPerDigit;
        multiplicationLeftHalfs <<= (shiftHalfs + shiftHalfs);
        multiplicationDiagonal <<= shiftHalfs;

        return multiplicationRightHalfs + multiplicationLeftHalfs + multiplicationDiagonal;
    }

    private BetterBigInteger AddLeadingZeros(BetterBigInteger number, int countDigit)
    {
        /* добивает ведущими нулями, чтобы всего оказалось countDigit цифр */
        
        var oldCountDigits = number.GetDigits().Length;
        var newDigits = new uint[countDigit];
        Array.Copy(number.GetDigits().ToArray(), newDigits, oldCountDigits);

        for (int i = oldCountDigits; i < countDigit; ++i) newDigits[i] = 0;

        return new BetterBigInteger(newDigits, number.IsNegative);
    }

    private int FindCountDigitsForDivide(BetterBigInteger a, BetterBigInteger b)
    {
        /* возвращает количество цифр, чтобы было удобно их разделить */
        
        int result = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        if (result % 2 == 0) return result;
        return result + 1;
    }
}