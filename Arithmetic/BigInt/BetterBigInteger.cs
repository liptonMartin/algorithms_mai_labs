using System.ComponentModel;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;

    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    private const int CountBits = sizeof(uint) * 8;
    private const int HalfDigit = (sizeof(uint) / 2) * 8;
    private const uint MaskForRightHalf = (1 << HalfDigit) - 1; // маска для правой половинки (единицы в правой части) 
    private const uint MaskForLeftHalf = MaskForRightHalf << HalfDigit; // маска для левой половинки (единицы в левой части)

    private const uint MaxDigit = MaskForLeftHalf | MaskForRightHalf; // максимальное число в c/c 2^32 
    
    public bool IsNegative => _signBit == 1;

    private enum DivisionMode
    {
        IntegerDivision,
        Remainder,
    }

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (!isNegative && digits.Length == 0) throw new InvalidDataInConstructorException("Отрицательный ноль ?");

        _signBit = isNegative ? 1 : 0;
        if (digits.Length <= 1) // Length == 0 || Length == 1 
        {
            _smallValue = digits.Length == 1 ? digits[0] : 0;
        }
        else
        {
            _data = digits;
        }
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        var listDigits = digits.ToList();
        if (!isNegative && listDigits.Count == 0) throw new InvalidDataInConstructorException("Отрицательный ноль ?");
        
        _signBit = isNegative ? 1 : 0;
        if (listDigits.Count <= 1)
        {
            _smallValue = listDigits.Count == 1 ? listDigits[0] : 0;
            _data = null;
        }

        else
        {
            _data = new uint[listDigits.Count];
            for (int i = 0; i < listDigits.Count; ++i)
            {
                _data[i] = listDigits[i];
            }
        }
    }

    public BetterBigInteger(string value, int radix)
    {
        if (radix < 2 || radix > 36) 
            throw new ArgumentOutOfRangeException(nameof(radix), "Основание должно быть в пределах [2..36]");
        
        int start = 0;
        if (value[start] == '+' || value[start] == '-')
        {
            _signBit = value[start] == '+' ? 0 : 1;
            start++;
        }
        if (start == value.Length) throw new InvalidDataInConstructorException("Знак без цифр");

        while (start < value.Length && value[start] == '0') start++;
        if (start == value.Length)
        {
            if (value[0] == '+' || value[0] == '-') throw new InvalidDataInConstructorException("Ноль со знаком ?");
            _smallValue = 0;
            _data = null;
            _signBit = 0;
            return;
        }

        var result = new BetterBigInteger([0]);
        BetterBigInteger bigRadix = new BetterBigInteger([(uint)radix]);
        for (int i = start; i < value.Length; ++i)
        {
            var curDigit = GetValueFromDigit(value[i], radix);
            result = result * bigRadix + curDigit;
        }

        var digits = result.GetDigits();
        if (digits.Length == 1)
        {
            _smallValue = digits[0];
            _data = null;
        }
        else
        {
            _data = digits.ToArray();
        }
    }
    
    private BetterBigInteger GetValueFromDigit(char digit, int radix)
    {
        var maxDigitInRadix = radix <= 10 ? '0' + radix - 1 : '9';
        var maxSmallLetterInRadix = 'a' + radix - 1;
        var maxBigLetterInRadix = 'A' + radix - 1;
        
        var result = 0;
        if (digit >= '0' && digit <= maxDigitInRadix) result = digit - '0';
        else if (radix > 10 && digit >= 'a' && digit <= maxSmallLetterInRadix) result = digit - 'a' + 10;
        else if (radix > 10 && digit >= 'A' && digit <= maxBigLetterInRadix) result = digit - 'A' + 10;
        else throw new InvalidDataInConstructorException($"Несоответствующий символ {digit} в с/c {radix}!");

        return new BetterBigInteger([(uint)result]);
    }
    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    public static BetterBigInteger AbsoluteBetterBigInteger(BetterBigInteger a) => 
        new BetterBigInteger(a.GetDigits().ToArray());

    public static BetterBigInteger GetTwosComplement(uint[] digits, bool isNegative, int countDigits)
    {
        /* получение дополнительного кода
         * в случае отрицательного числа, у него будет ровно countDigits цифр, спереди заполненные единицами
         */
        
        if (!isNegative) return new BetterBigInteger(digits);
        
        return InvertAllBits(digits, countDigits) + new BetterBigInteger([1]);
    }

    private static BetterBigInteger FromTwosComplement(uint[] digits, bool isNegative)
    {
        /* переводит из дополнительного кода в систему счисления с основанием 2^32 */

        if (!isNegative) return new BetterBigInteger(digits);
        
        BetterBigInteger result = InvertAllBits(digits, digits.Length) + new BetterBigInteger([1]); // модуль числа
        
        result = new BetterBigInteger(result.GetDigits().ToArray(), isNegative);
        result.RemoveForwardZeros();
        return result;
    }

    private static BetterBigInteger InvertAllBits(uint[] oldDigits, int countDigits)
    {
        /* меняет все биты на противоположные, добавляет единицы в начало,
         в итоговом числе будет ровно countDigits цифр */
        if (oldDigits.Length > countDigits) 
            throw new ArithmeticException("Число цифр итогового числа меньше чем само число!");
        
        uint[] newDigits = new uint[countDigits];

        for (int i = 0; i < oldDigits.Length; ++i) newDigits[i] = ~oldDigits[i]; // инвертируем биты цифры
        for (int i = oldDigits.Length; i < countDigits; ++i) newDigits[i] = MaxDigit; // все единицы
        
        BetterBigInteger result = new BetterBigInteger(newDigits);
        result.RemoveForwardZeros(); // чисто в теории число могло быть только из единиц, после инвертирования стали все нули
        return result;
    }

    public bool IsZero() => _data == null && _smallValue == 0;
    
    public int CompareTo(IBigInteger? other)
    {
        if (other == null) return 1;

        if (!this.IsNegative && other.IsNegative) return 1;
        if (this.IsNegative && !other.IsNegative) return -1;

        int result = CompareToInner(other);
        return _signBit == 1 ? -result : result;
    }
    
    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(_signBit);
        foreach (var item in GetDigits())
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        bool sign = false;
        if (a.IsNegative && !b.IsNegative)
        {
            // -a + b = b - a
            return b - (-a);
        }

        if (!a.IsNegative && b.IsNegative)
        {
            // a + (-b) = a - b
            return a - (-b);
        }

        if (a.IsNegative && b.IsNegative)
        {
            // -a + (-b)
            sign = true;
            a = -a;
            b = -b;
        }

        return HandleAdditionOperation(a, b, sign);
    }

    private static BetterBigInteger HandleAdditionOperation(BetterBigInteger a, BetterBigInteger b, bool sign)
    {
        var firstNumberDigits = a.GetDigits();
        var secondNumberDigits = b.GetDigits();

        int n = Math.Max(firstNumberDigits.Length, secondNumberDigits.Length);

        uint[] digits = new uint[n];

        uint accumulator = 0;
        for (int i = 0; i < n; ++i)
        {
            uint firstDigit = TryGetDigit(firstNumberDigits, i);
            uint secondDigit = TryGetDigit(secondNumberDigits, i);

            uint result = SumDigits(firstDigit, secondDigit, ref accumulator);
            
            digits[i] = result;
        }

        if (accumulator == 1)
        {
            uint[] newDigits = new uint[n + 1];
            Array.Copy(digits, newDigits, digits.Length);
            newDigits[n] = 1;
            digits = newDigits;
        }

        return new BetterBigInteger(digits, sign);
    }

    private static uint SumDigits(uint firstDigit, uint secondDigit, ref uint accumulator)
    {
        /* суммирует две цифры с помощью метода половинок
         * 
         * firstDigit:: первая цифра
         * secondDigit:: вторая цифра
         * accumulator:: учитывается при сложении, после вычислений хранит новое значение
         *
         * return:: возвращается сумма, при этом если было переполнение, то это хранится в accumulator
         */
        
        uint rightHalfFirstDigit = firstDigit & MaskForRightHalf; // правая половинка первого числа
        uint rightHalfSecondDigit = secondDigit & MaskForRightHalf; // правая половинка второго числа
        
        uint resultRightHalfs = SumHalfDigits(rightHalfFirstDigit, rightHalfSecondDigit, ref accumulator);
        
        uint leftHalfFirstDigit = firstDigit & MaskForLeftHalf; // левая половинка первого числа
        uint leftHalfSecondDigit = secondDigit & MaskForLeftHalf; // левая половинка второго числа
        
        leftHalfFirstDigit = leftHalfFirstDigit >> HalfDigit;
        leftHalfSecondDigit = leftHalfSecondDigit >> HalfDigit;
        
        uint resultLeftHalfs = SumHalfDigits(leftHalfFirstDigit, leftHalfSecondDigit, ref accumulator) << HalfDigit;
        return resultRightHalfs + resultLeftHalfs;
    }

    private static uint SumHalfDigits(uint firstHalfDigit, uint secondHalfDigit, ref uint accumulator)
    {
        /* суммирует две половинки
         * 
         * firstHalfDigit:: половинка, сдвинутая максимальная вправо
         * secondHalfDigit:: половинка, сдвинутая максимальная вправо
         * ref accumulator:: учитывается при сложении, после вычислений хранит новое значение
         * 
         * return:: получается число, которое в последствии нужно будет сдвинуть при случае (сама функция не сдвигает)
         */
        
        uint result = firstHalfDigit + secondHalfDigit + accumulator;
        accumulator = result >> HalfDigit; // сдвигаю аккумулятор максимально вправо
        return result & MaskForRightHalf;
    }

    private static uint TryGetDigit(ReadOnlySpan<uint> digits, int i) => i < digits.Length ? digits[i] : 0;

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        if (!a.IsNegative && !b.IsNegative)
        {
            // a - b
            if (a < b)
            {
                // тогда делаем так: -(b - a)
                return -HandleSubtractionOperation(b, a);
            }
            return HandleSubtractionOperation(a, b);
        }

        if (a.IsNegative && !b.IsNegative)
        {
            // -a -b = - (a+b)
            return -(-a + b);
        }

        if (a.IsNegative && b.IsNegative)
        {
            // -a - (-b) = b - a
            return -b + a;
        }
        
        // a - (-b) = a + b
        return a + (-b);
    }

    private static BetterBigInteger HandleSubtractionOperation(BetterBigInteger a, BetterBigInteger b)
    {
        /* здесь a >= b */
        if (a < b) throw new ArithmeticException("Ошибка при вычислении: первое число должно быть меньше второго!");

        var digitsA =  a.GetDigits();
        var digitsB = b.GetDigits();

        uint accumulator = 0;
        var digits = new uint[digitsA.Length]; 

        for (int i = 0; i < digitsA.Length; ++i) // i < digitsA.Length потому что a >= b
        {
            uint digitB = TryGetDigit(digitsB, i);
            digits[i] = MinusDigits(digitsA[i], digitB, ref accumulator);
        }
        
        if (accumulator == 1) throw new ArithmeticException("Ошибка при вычислении!");
        
        BetterBigInteger result = new BetterBigInteger(digits);
        result.RemoveForwardZeros();
        return result;
    }

    private static uint MinusDigits(uint firstDigit, uint secondDigit, ref uint accumulator)
    {
        /* вычитает две цифры */

        if (accumulator == 1)
        {
            secondDigit += accumulator;
            accumulator = 0;
            if (secondDigit == 0) accumulator = 1; // произошло переполнение
        }
        
        if (secondDigit > firstDigit)
        {
            accumulator = 1;
        }
        
        return firstDigit - secondDigit;
    }
    
    public static BetterBigInteger operator -(BetterBigInteger a) => 
        new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero()) throw new DivideByZeroException("Деление на ноль!!!");
        
        if ((!a.IsNegative && !b.IsNegative) || (a.IsNegative && b.IsNegative))
        {
            // a / b || -a / -b
            return HandleDivisionOperation(AbsoluteBetterBigInteger(a), AbsoluteBetterBigInteger(b), DivisionMode.IntegerDivision);
        }
        // -a / b || a / -b
        return -HandleDivisionOperation(AbsoluteBetterBigInteger(a), AbsoluteBetterBigInteger(b), DivisionMode.IntegerDivision);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero()) throw new DivideByZeroException("Деление на ноль!!!");
            
        if (a.IsNegative) return -HandleDivisionOperation(AbsoluteBetterBigInteger(a), AbsoluteBetterBigInteger(b), DivisionMode.Remainder);
        return HandleDivisionOperation(a,  AbsoluteBetterBigInteger(b), DivisionMode.Remainder);
    }

    private static BetterBigInteger HandleDivisionOperation(BetterBigInteger dividend, BetterBigInteger divisor, DivisionMode mode)
    {
        var digitsDividend = dividend.GetDigits();
        
        BetterBigInteger accumulator = new BetterBigInteger([0]);
        int indexDigitDivident = digitsDividend.Length - 1;
        while (accumulator < divisor && indexDigitDivident != -1)
        {
            accumulator.AddDigit(digitsDividend[indexDigitDivident]);
            --indexDigitDivident;
        }

        BetterBigInteger quotient = new BetterBigInteger([0]);
        while (accumulator >= divisor)
        {
            uint digitQuotient = BinarySearchDigitQuotient(accumulator, divisor);
            BetterBigInteger bigDigitQuotient = new BetterBigInteger([digitQuotient]);
            
            quotient.AddDigit(digitQuotient);
            
            BetterBigInteger currentMultiply = bigDigitQuotient * divisor;
            accumulator -= currentMultiply;

            if (accumulator >= divisor) throw new ArithmeticException("Неправильно подобрали цифру частного!");

            bool isAdded = false; // флаг, который проверяет, снесли ли уже первую цифру
            while (accumulator < divisor && indexDigitDivident != -1)
            {
                accumulator.AddDigit(digitsDividend[indexDigitDivident]);
                if (isAdded || accumulator.IsZero()) quotient.AddDigit(0);
                
                --indexDigitDivident;
                isAdded = true;
            }
            accumulator.RemoveForwardZeros(); // в теории могли добавить незначащие нули
        }

        switch (mode)
        {
            case DivisionMode.IntegerDivision: 
                return quotient;
            case DivisionMode.Remainder:
                return accumulator;
        }

        throw new InvalidEnumArgumentException("Поддерживается только деление нацело или остаток");
    }

    private void AddDigit(uint digit)
    {
        if (_smallValue == 0 && _data == null)
        {
            _smallValue = digit;
            return;
        } 
        uint[] newDigits = new uint[GetDigits().Length + 1];
        Array.Copy(GetDigits().ToArray(), 0, newDigits, 1, GetDigits().Length);
        newDigits[0] = digit;
        _data = newDigits;
    }

    private static uint BinarySearchDigitQuotient(BetterBigInteger accumulator, BetterBigInteger divisor)
    {
        /* подбирает результат деления dividend на divisor путем бинарного поиска
         *
         * в эту функцию dividend и divisor должны попасть так, что при делении максимум получается только один uint
         */
        uint l = 1;
        uint r = MaskForLeftHalf | MaskForRightHalf; // максимальное число 
        uint result = l;
        while (l <= r)
        {
            uint m = l + (r - l) / 2;
            BetterBigInteger bigM = new BetterBigInteger([m]);
        
            if (divisor * bigM < accumulator)
            {
                result = m;
                l = m + 1;
            }
            else if (divisor * bigM == accumulator) return m;
            else r = m - 1;
            
        }
        
        return result;
    }


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        // TODO: делегировать после реализации других умножений!
        SimpleMultiplier multiplier = new SimpleMultiplier();
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return -a - new BetterBigInteger([1]);
    }
    

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        bool sign = a.IsNegative && b.IsNegative;
        int maxCountDigit = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        uint[] digits = new uint[maxCountDigit];

        /* дополнительный код */
        a = GetTwosComplement(a.GetDigits().ToArray(), a.IsNegative, maxCountDigit);
        b = GetTwosComplement(b.GetDigits().ToArray(), b.IsNegative, maxCountDigit);
        
        for (int i = 0; i < maxCountDigit; ++i)
        {
            uint digitA = TryGetDigit(a.GetDigits(), i);
            uint digitB = TryGetDigit(b.GetDigits(), i);

            digits[i] = digitA & digitB;
        }
        
        BetterBigInteger result = FromTwosComplement(digits, sign);
        return result;
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        bool sign = a.IsNegative || b.IsNegative;
        int maxCountDigit = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        uint[] digits = new uint[maxCountDigit];
        
        /* дополнительный код */
        a = GetTwosComplement(a.GetDigits().ToArray(), a.IsNegative, maxCountDigit);
        b = GetTwosComplement(b.GetDigits().ToArray(), b.IsNegative, maxCountDigit);

        for (int i = 0; i < maxCountDigit; ++i)
        {
            uint digitA = TryGetDigit(a.GetDigits(), i);
            uint digitB = TryGetDigit(b.GetDigits(), i);

            digits[i] = digitA | digitB;
        }
        
        BetterBigInteger result = FromTwosComplement(digits, sign);
        return result;
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        bool sign = (a.IsNegative && !b.IsNegative) || (!a.IsNegative && b.IsNegative);
        int maxCountDigit = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        uint[] digits = new uint[maxCountDigit];
        
        /* дополнительный код */
        a = GetTwosComplement(a.GetDigits().ToArray(), a.IsNegative, maxCountDigit);
        b = GetTwosComplement(b.GetDigits().ToArray(), b.IsNegative, maxCountDigit);

        for (int i = 0; i < maxCountDigit; ++i)
        {
            uint digitA = TryGetDigit(a.GetDigits(), i);
            uint digitB = TryGetDigit(b.GetDigits(), i);

            digits[i] = digitA ^ digitB;
        }
        
        BetterBigInteger result = FromTwosComplement(digits, sign);
        return result;
    }
    
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0) return a >> -shift;

        if (shift == 0) return a;
        
        int divide = shift / CountBits; // насколько сдвинутся в целом все числа
        shift %= CountBits; // новый сдвиг (он только внутри цифр)
        
        var oldDigits =  a.GetDigits();
        var digits =  new uint[oldDigits.Length + divide];

        for (int i = 0; i < oldDigits.Length; ++i)
        {
            digits[i + divide] = oldDigits[i];
        }
        
        /* здесь точно shift < sizeof(uint) * 8
         * но возможен сдвиг среди цифр, поэтому заводим accumulator         
         */

        if (shift == 0) return new BetterBigInteger(digits, a.IsNegative);
        
        var countToShift = CountBits - shift;
        uint accumulator = 0;
        for (int i = 0; i < digits.Length; ++i)
        {
            var temp = digits[i] >> countToShift;
            
            digits[i] <<= shift;
            digits[i] |= accumulator;
            accumulator = temp;
        }
        
        if (accumulator != 0)
        {
            var newDigits = new uint[digits.Length + 1];
            Array.Copy(digits, 0, newDigits, 0, digits.Length);
            newDigits[digits.Length] = accumulator;
            digits = newDigits;
        }
        
        return new BetterBigInteger(digits, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift < 0) return a << -shift;
        
        if (shift == 0) return a;
        
        var oldDigits = a.GetDigits();
        
        if (a.IsNegative)
        {
            // формула -n >> shift = -ceil(x / 2^n) = - ( (x + 2^n - 1) / 2^n )
            BetterBigInteger divisor = new BetterBigInteger([1]);
            divisor <<= shift;
            divisor -= new BetterBigInteger([1]);
            BetterBigInteger positiveA = new BetterBigInteger(oldDigits.ToArray(), false);
            BetterBigInteger ceilA = (positiveA + divisor) >> shift;
            var newDigits = ceilA.GetDigits().ToArray();
            return new BetterBigInteger(newDigits, true);
        }
        
        int divide = shift / CountBits; // насколько сдвинутся в целом все числа
        shift %= CountBits; // новый сдвиг (он только внутри цифр)

        if (divide >= oldDigits.Length) return new BetterBigInteger([0]);
        
        var digits = new uint[oldDigits.Length - divide];

        for (int i = divide; i < oldDigits.Length; ++i)
        {
            digits[i - divide] = oldDigits[i];
        }
        
        if (shift == 0) return new BetterBigInteger(digits, a.IsNegative);
        
        var countToShift = CountBits - shift;
        uint accumulator = 0;
        for (int i = digits.Length - 1; i >= 0; --i)
        {
            var temp = digits[i] << countToShift;

            digits[i] >>= shift;
            digits[i] |= accumulator;
            accumulator = temp;
        }

        BetterBigInteger result = new BetterBigInteger(digits, a.IsNegative);
        result.RemoveForwardZeros();
        return result;
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "Основание должно быть в пределах [2..36]");

        if (IsZero()) return "0";
        
        var bigRadix = new BetterBigInteger(radix.ToString(), 10);
        var number = AbsoluteBetterBigInteger(this);

        var result = "";
        while (!number.IsZero())
        {
            BetterBigInteger remainder = number % bigRadix;
            uint digitRemainder = remainder.GetDigits()[0];
            result += DigitToChar(digitRemainder);
            
            number /= bigRadix;
        }
        
        if (IsNegative) result += "-";

        return new string(result.Reverse().ToArray());
    }

    private static char DigitToChar(uint digit)
    {
        if (digit < 10)
            return (char)('0' + digit);
        return (char)('A' + (digit - 10));
    }

    internal void RemoveForwardZeros()
    {
        /* удаляет ведущие нули */

        if (_data == null) return; // если число состоит из одного числа, удалять ничего не нужно

        int i = _data.Length - 1;
        while (_data[i] == 0 && i != 0)
        {
            --i;
        }
        // i - либо первая цифра, либо первая цифра, отличная от нуля
        uint[] newDigits = new uint[i + 1];
        
        Array.Copy(_data, newDigits, i + 1);

        if (newDigits.Length == 1)
        {
            _data = null;
            _smallValue = newDigits[0];
        }
        else
        {
            _data = newDigits;
        }
    }

    private int CompareToInner(IBigInteger other)
    {
        var digitsFirstNumber = this.GetDigits();
        var digitsSecondNumber = other.GetDigits();
        
        if (digitsFirstNumber.Length > digitsSecondNumber.Length) return 1;
        if (digitsFirstNumber.Length < digitsSecondNumber.Length) return -1;
        
        for (int i = digitsFirstNumber.Length - 1; i >= 0; --i)
        {
            if (digitsFirstNumber[i] == digitsSecondNumber[i]) continue;
            
            return digitsFirstNumber[i] >  digitsSecondNumber[i] ? 1 : -1;
        }

        return 0;
    }
    private class InvalidDataInConstructorException : Exception
    {
        public InvalidDataInConstructorException() { }
        public InvalidDataInConstructorException(string message) : base(message) { }
    }

}