#pragma once
#include <math.h>
#include <atomic>
#include <algorithm>
#include <assert.h>
#include <string>
#include <iostream>

//#include <bits/stdc++.h>

//MIT license: https://github.com/liulilittle/Int128
namespace liulilittle
{
#pragma pack(push, 1)
    class Int128
    {
    public:
        unsigned long long                                              lo;
        signed long long                                                hi;

    public:
        inline Int128() : lo(0), hi(0) {};
        inline Int128(signed char value);
        inline Int128(signed short int value);
        inline Int128(signed int value);
        inline Int128(signed long int value);
        inline Int128(signed long long value);
        inline Int128(bool value) : lo(value), hi(0) {};
        inline Int128(unsigned char value) : lo(value), hi(0) {};
        inline Int128(unsigned short value) : lo(value), hi(0) {};
        inline Int128(unsigned int value) : lo(value), hi(0) {};
        inline Int128(unsigned long int value) : lo(value), hi(0) {};
        inline Int128(unsigned long long value) : lo(value), hi(0) {};
        inline Int128(const Int128& value) : lo(value.lo), hi(value.hi) {};
        inline Int128(const signed long long& high, const unsigned long long& low) : lo(low), hi(high) {};

    private:
        inline Int128(int sign, unsigned int* ints, int intslen);

    public:
        inline Int128& operator = (const Int128& value);
        inline friend bool                                              operator == (const Int128& left, const Int128& right);
        inline friend bool                                              operator != (const Int128& left, const Int128& right);
        inline friend bool                                              operator < (const Int128& left, const Int128& right);
        inline friend bool                                              operator > (const Int128& left, const Int128& right);
        inline friend bool                                              operator <= (const Int128& left, const Int128& right);
        inline friend bool                                              operator >= (const Int128& left, const Int128& right);
        inline friend Int128                                            operator + (const Int128& left, const Int128& right);
        inline friend Int128                                            operator - (const Int128& left, const Int128& right);
        inline friend Int128                                            operator * (const Int128& left, const Int128& right);
        inline friend Int128                                            operator / (const Int128& left, const Int128& right);
        inline friend Int128                                            operator % (const Int128& left, const Int128& right);
        inline Int128                                                   operator - () const;
        inline Int128& operator ++ () const;
        inline Int128                                                   operator ++ (int) const;
        inline Int128& operator -- () const;
        inline Int128                                                   operator -- (int) const;
        inline Int128& operator += (const Int128& value);
        inline Int128& operator -= (const Int128& value);
        inline friend std::ostream& operator << (std::ostream& out, const Int128& value);
        inline friend std::istream& operator >> (std::istream& in, const Int128& value);
        inline friend Int128                                            operator ~ (const Int128& value);
        inline friend Int128                                            operator | (const Int128& left, const Int128& right);
        inline friend Int128                                            operator & (const Int128& left, const Int128& right);
        inline friend Int128                                            operator ^ (const Int128& left, const Int128& right);
        inline friend Int128                                            operator << (const Int128& value, int shift);
        inline friend Int128                                            operator >> (const Int128& value, int shift);

#if defined(WIN32) || __cplusplus >= 201103L
    public:
        explicit inline                                                 operator bool() const;
        explicit inline                                                 operator signed char() const;
        explicit inline                                                 operator signed short int() const;
        explicit inline                                                 operator signed int() const;
        explicit inline                                                 operator signed long() const;
        explicit inline                                                 operator signed long long() const;
        explicit inline                                                 operator unsigned char() const;
        explicit inline                                                 operator unsigned short() const;
        explicit inline                                                 operator unsigned int() const;
        explicit inline                                                 operator unsigned long() const;
        explicit inline                                                 operator unsigned long long() const;
#endif

    public:
        inline int                                                      Sign();
        inline std::string                                              ToString();
        inline std::string                                              ToString(int radix);
        inline std::string                                              ToHex();
        inline std::string                                              ToBinary();

    private:
        inline static Int128                                            Multiply(Int128 left, Int128 right);
        inline static Int128                                            DivRem(Int128 dividend, Int128 divisor, Int128& remainder);

    private:
        inline void                                                     Negate();
        inline static int                                               GetNormalizeShift(unsigned int value);
        inline static int                                               GetLength(unsigned int* uints, int uintslen);
        inline static void                                              Normalize(unsigned int* u, int l, unsigned int* un, int shift);
        inline static void                                              Unnormalize(unsigned int* un, unsigned int* r, int shift);
        inline static void                                              DivModUnsigned(unsigned int* u, unsigned int* v, unsigned int*& q, unsigned int*& r);

    public:
        static const unsigned long long                                 Base32 = 0x100000000;
        static const unsigned long long                                 NegativeSignMask = 0x1ull << 63;
    };
#pragma pack(pop)

    inline Int128::Int128(signed char value)
    {
        hi = (unsigned long long)(value < 0 ? ~0 : 0);
        lo = (unsigned long long)value;
    }

    inline Int128::Int128(signed short int value)
    {
        hi = (unsigned long long)(value < 0 ? ~0 : 0);
        lo = (unsigned long long)value;
    }

    inline Int128::Int128(signed int value)
    {
        hi = (unsigned long long)(value < 0 ? ~0 : 0);
        lo = (unsigned long long)value;
    }

    inline Int128::Int128(signed long int value)
    {
        hi = (unsigned long long)(value < 0 ? ~0 : 0);
        lo = (unsigned long long)value;
    }

    inline Int128::Int128(signed long long value)
    {
        hi = (unsigned long long)(value < 0 ? ~0 : 0);
        lo = (unsigned long long)value;
    }

    inline Int128::Int128(int sign, unsigned int* ints, int intslen)
    {
        unsigned long long value[2];
        memset(value, 0, sizeof(value));
        for (int i = 0; i < intslen && i < 4; i++)
        {
            memcpy((i * 4) + (char*)value, ints + i, 4);
        }

        hi = value[1];
        lo = value[0];

        if (sign < 0 && (hi > 0 || lo > 0))
        {
            Negate();
            hi |= NegativeSignMask;
        }
    }

    inline Int128& Int128::operator=(const Int128& value)
    {
        lo = value.lo;
        hi = value.hi;
        return *this;
    }

    inline Int128 Int128::operator-() const
    {
        Int128 x = *this;
        x.Negate();
        return x;
    }

    inline Int128& Int128::operator++() const
    {
        Int128& r = const_cast<Int128&>(*this);
        r += 1;
        return r;
    }

    inline Int128 Int128::operator++(int) const
    {
        Int128& c = const_cast<Int128&>(*this);
        Int128 r = c;
        c += 1;
        return r;
    }

    inline Int128& Int128::operator--() const
    {
        Int128& r = const_cast<Int128&>(*this);
        r -= 1;
        return r;
    }

    inline Int128 Int128::operator--(int) const
    {
        Int128& c = const_cast<Int128&>(*this);
        Int128 r = c;
        c -= 1;
        return r;
    }

    inline Int128& Int128::operator+=(const Int128& value)
    {
        *this = *this + value;
        return *this;
    }

    inline Int128& Int128::operator-=(const Int128& value)
    {
        *this = *this - value;
        return *this;
    }

    inline bool operator==(const Int128& left, const Int128& right)
    {
        return (left.lo == right.lo) && (left.hi == right.hi);
    }

    inline bool operator!=(const Int128& left, const Int128& right)
    {
        return !(left == right);
    }

    inline bool operator<(const Int128& left, const Int128& right)
    {
        if (left.hi != right.hi)
        {
            return left.hi < right.hi;
        }
        return left.lo < right.lo;
    }

    inline bool operator>(const Int128& left, const Int128& right)
    {
        if (left.hi != right.hi)
        {
            return left.hi > right.hi;
        }
        return left.lo > right.lo;
    }

    inline bool operator<=(const Int128& left, const Int128& right)
    {
        return (left == right) || (left < right);
    }

    inline bool operator>=(const Int128& left, const Int128& right)
    {
        return (left == right) || (left > right);
    }

    inline Int128 operator+(const Int128& left, const Int128& right)
    {
        Int128 value;
        value.hi = left.hi + right.hi;
        value.lo = left.lo + right.lo;
        // Carry
        if (value.lo < left.lo)
        {
            value.hi++;
        }
        return value;
    }

    inline Int128 operator-(const Int128& left, const Int128& right)
    {
        return left + (-right);
    }

    inline Int128 operator*(const Int128& left, const Int128& right)
    {
        return Int128::Multiply(left, right);
    }

    inline Int128 operator/(const Int128& left, const Int128& right)
    {
        Int128 remainder = 0;
        return Int128::DivRem(left, right, remainder);
    }

    inline Int128 operator%(const Int128& left, const Int128& right)
    {
        Int128 remainder = 0;
        Int128::DivRem(left, right, remainder);
        return remainder;
    }

    inline std::ostream& operator<<(std::ostream& out, const Int128& value)
    {
        return out.write((char*)&value.lo, 16);
    }

    inline std::istream& operator>>(std::istream& in, const Int128& value)
    {
        return in.read((char*)&value.lo, 16);
    }

    inline Int128 operator~(const Int128& value)
    {
        return Int128(~value.hi, ~value.lo);
    }

    inline Int128 operator|(const Int128& left, const Int128& right)
    {
        if (left == 0)
        {
            return right;
        }

        if (right == 0)
        {
            return left;
        }

        Int128 R = left;
        R.hi |= right.hi;
        R.lo |= right.lo;
        return R;
    }

    inline Int128 operator&(const Int128& left, const Int128& right)
    {
        if (left == 0)
        {
            return right;
        }

        if (right == 0)
        {
            return left;
        }

        Int128 R = left;
        R.hi &= right.hi;
        R.lo &= right.lo;
        return R;
    }

    inline Int128 operator^(const Int128& left, const Int128& right)
    {
        if (left == 0)
        {
            return right;
        }

        if (right == 0)
        {
            return left;
        }

        Int128 R = left;
        R.hi ^= right.hi;
        R.lo ^= right.lo;
        return R;
    }

    inline Int128 operator<<(const Int128& value, int shift)
    {
        if (shift == 0 || value == 0)
        {
            return value;
        }

        if (shift < 0)
        {
            return value >> -shift;
        }

        unsigned long long* values = (unsigned long long*) & value.lo;

        shift = shift % 128;

        int shiftOffset = shift / 64;
        int bshift = shift % 64;

        unsigned long long shifted[2];
        memset(shifted, 0, sizeof(shifted));

        for (int i = 0; i < 2; i++)
        {
            int ishift = i + shiftOffset;
            if (ishift >= 2)
            {
                continue;
            }

            shifted[ishift] |= values[i] << bshift;
            if (bshift > 0 && i - 1 >= 0)
            {
                shifted[ishift] |= values[i - 1] >> (64 - bshift);
            }
        }

        return Int128((signed long long)(shifted[1]), shifted[0]); // lo is stored in array entry 0  
    }

    inline Int128 operator>>(const Int128& value, int shift)
    {
        if (shift == 0 || value == 0)
        {
            return value;
        }

        if (shift < 0)
        {
            return value << -shift;
        }

        unsigned long long* values = (unsigned long long*) & value.lo;
        shift = shift % 128;     // This is the defined behavior of shift. Shifting by greater than the number of bits uses a mod

        //
        //  First, shift over by full ulongs. This could be optimized a bit for longer arrays (if shifting by multiple longs, we do more copies 
        //  than needed), but for short arrays this is probably the best way to go
        //
        while (shift >= 64)
        {
            for (int i = 0; i < 1; i++)
            {
                values[i] = values[i + 1];
            }

            values[1] = (unsigned long long)((signed long long)values[1] >> (64 - 1));    // Preserve sign of upper long, will either be 0 or all f's
            shift -= 64;
        }

        //
        //  Now, we just have a sub-long shift left to do (shift will be < 64 at this point)
        //
        if (shift == 0)
        {
            return value;
        }

        int bshift = 64 - shift;

        //
        //  In right shifting, upper val is a special case because we need to preserve the sign bits, and because we don't need to or in
        //  any other values
        //
        unsigned long long shifted[2];
        memset(shifted, 0, sizeof(shifted));

        shifted[1] = (unsigned long long)((signed long long)values[1] >> shift);    // Preserve sign of upper long
        for (int i = 0; i < 1; i++)
        {
            shifted[i] = values[i] >> shift;                   // Unsigned, so upper bits stay zero
            shifted[i] |= (values[i + 1] << bshift);
        }

        return Int128((signed long long)(shifted[1]), shifted[0]); // lo is stored in array entry 0  
    }

#if defined(WIN32) || __cplusplus >= 201103L
    inline Int128::operator bool() const
    {
        return lo != 0 || hi != 0;
    }

    inline Int128::operator signed char() const
    {
        return (signed char)lo;
    }

    inline Int128::operator signed short int() const
    {
        return (signed short int)lo;
    }

    inline Int128::operator signed int() const
    {
        return (signed int)lo;
    }

    inline Int128::operator signed long() const
    {
        return (signed long)lo;
    }

    inline Int128::operator signed long long() const
    {
        return (signed long long)lo;
    }

    inline Int128::operator unsigned int() const
    {
        return (unsigned int)lo;
    }

    inline Int128::operator unsigned long() const
    {
        return (unsigned long)lo;
    }

    inline Int128::operator unsigned char() const
    {
        return (unsigned char)lo;
    }

    inline Int128::operator unsigned short() const
    {
        return (unsigned short)lo;
    }

    inline Int128::operator unsigned long long() const
    {
        return (unsigned long long)lo;
    }
#endif

    inline int Int128::Sign()
    {
        if (hi == 0 && lo == 0)
        {
            return 0;
        }

        return ((hi & NegativeSignMask) == 0) ? 1 : -1;
    }

    inline std::string Int128::ToString()
    {
        return ToString(10);
    }

    inline std::string Int128::ToString(int radix)
    {
        static char hex[] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (radix < 2)
        {
            radix = 10;
        }
        else if (radix > 36)
        {
            radix = 36;
        }
        char buf[536];
        char* sz = buf + sizeof(buf);
        *--sz = '\x0';
        Int128 n = Sign() < 0 ? -(*this) : *this;
        if (n.lo == 0 && n.hi == 0)
        {
            buf[0] = '0';
            buf[1] = '\x0';
            return buf;
        }
        Int128 x = radix;
        while (0 != n.Sign())
        {
            Int128 r = 0;
            n = Int128::DivRem(n, x, r);
            if (r.lo > 0 || n.Sign() != 0)
            {
                *--sz = hex[r.lo];
            }
        }
        if (Sign() < 0 && *sz != '0')
        {
            *--sz = '-';
        }
        return sz;
    }

    inline std::string Int128::ToHex()
    {
        char bf[536];
        char* sz = bf;
        unsigned char* p = (unsigned char*)&lo;
        for (int i = 15; i >= 0; --i)
        {
            unsigned char ch = p[i];
            for (int j = 0; j < 2; j++)
            {
                char pi = (char)(ch & 0xf);
                if (pi >= 10)
                    pi = 'A' + (pi - 10);
                else
                    pi = '0' + pi;
                sz[1 - j] = pi;
                ch = ch >> 4;
            }
            sz += 2;
        }
        *sz++ = '\x0';
        return bf;
    }

    inline std::string Int128::ToBinary()
    {
        char bf[536];
        char* sz = bf;
        unsigned char* p = (unsigned char*)&lo;
        for (int i = 15; i >= 0; --i)
        {
            unsigned char ch = p[i];
            for (int j = 0; j < 8; j++)
            {
                if (ch & 0x01)
                    sz[7 - j] = '1';
                else
                    sz[7 - j] = '0';
                ch = ch >> 1;
            }
            sz += 8;
        }
        *sz++ = '\x0';
        return bf;
    }

    inline Int128 Int128::Multiply(Int128 left, Int128 right)
    {
        int leftSign = left.Sign();
        left = leftSign < 0 ? -left : left;

        int rightSign = right.Sign();
        right = rightSign < 0 ? -right : right;

        unsigned int xInts[4];
        unsigned int yInts[4];
        memcpy(xInts, &left.lo, 16);
        memcpy(yInts, &right.lo, 16);

        unsigned int mulInts[8] = { 0 };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            unsigned long long remainder = 0;
            for (int j = 0; j < 4; j++)
            {
                unsigned int yi = yInts[j];
                remainder = remainder + (unsigned long long)xInts[i] * yi + mulInts[index];
                mulInts[index++] = (unsigned int)remainder;
                remainder = remainder >> 32;
            }

            while (remainder != 0)
            {
                remainder += mulInts[index];
                mulInts[index++] = (unsigned int)remainder;
                remainder = remainder >> 32;
            }
        }

        return Int128(leftSign * rightSign, mulInts, 8);
    }

    inline int Int128::GetLength(unsigned int* uints, int uintslen)
    {
        int index = uintslen - 1;
        while ((index >= 0) && (uints[index] == 0))
            index--;
        index = index < 0 ? 0 : index;
        return index + 1;
    }

    inline Int128 Int128::DivRem(Int128 dividend, Int128 divisor, Int128& remainder)
    {
        if (divisor == 0 || dividend == 0)
        {
            return 0; // DivideByZeroException
        }

        int dividendSign = dividend.Sign();
        dividend = dividendSign < 0 ? -dividend : dividend;

        int divisorSign = divisor.Sign();
        divisor = divisorSign < 0 ? -divisor : divisor;

        unsigned int aquotient[4] = { 0 };
        unsigned int arem[4] = { 0 };

        unsigned int* quotient = aquotient;
        unsigned int* rem = arem;
        unsigned int* u = (unsigned int*)&dividend.lo;
        unsigned int* v = (unsigned int*)&divisor.lo;
        Int128::DivModUnsigned(u, v, quotient, rem);

        remainder = Int128(1, rem, 4);
        return Int128(dividendSign * divisorSign, quotient, 4);
    }

    inline void Int128::Negate()
    {
        hi = ~hi;
        lo = ~lo;
        (*this) += 1;
    }

    inline int Int128::GetNormalizeShift(unsigned int value)
    {
        int shift = 0;

        if ((value & 0xFFFF0000) == 0)
        {
            value <<= 16;
            shift += 16;
        }
        if ((value & 0xFF000000) == 0)
        {
            value <<= 8;
            shift += 8;
        }
        if ((value & 0xF0000000) == 0)
        {
            value <<= 4;
            shift += 4;
        }
        if ((value & 0xC0000000) == 0)
        {
            value <<= 2;
            shift += 2;
        }
        if ((value & 0x80000000) == 0)
        {
            value <<= 1;
            shift += 1;
        }

        return shift;
    }

    inline void Int128::Normalize(unsigned int* u, int l, unsigned int* un, int shift)
    {
        unsigned int carry = 0;
        int i;
        if (shift > 0)
        {
            int rshift = 32 - shift;
            for (i = 0; i < l; i++)
            {
                unsigned int ui = u[i];
                un[i] = (ui << shift) | carry;
                carry = ui >> rshift;
            }
        }
        else
        {
            for (i = 0; i < l; i++)
            {
                un[i] = u[i];
            }
        }

        while (i < 4)
        {
            un[i++] = 0;
        }

        if (carry != 0)
        {
            un[l] = carry;
        }
    }

    inline void Int128::Unnormalize(unsigned int* un, unsigned int* r, int shift)
    {
        int length = 4;
        if (shift > 0)
        {
            int lshift = 32 - shift;
            unsigned int carry = 0;
            for (int i = length - 1; i >= 0; i--)
            {
                unsigned int uni = un[i];
                r[i] = (uni >> shift) | carry;
                carry = (uni << lshift);
            }
        }
        else
        {
            for (int i = 0; i < length; i++)
            {
                r[i] = un[i];
            }
        }
    }

    inline void Int128::DivModUnsigned(unsigned int* u, unsigned int* v, unsigned int*& q, unsigned int*& r)
    {
        int m = GetLength(u, 4);
        int n = GetLength(v, 4);

        if (n <= 1)
        {
            //  Divide by single digit
            //
            unsigned long long rem = 0;
            unsigned int v0 = v[0];

            for (int j = m - 1; j >= 0; j--)
            {
                rem *= Base32;
                rem += u[j];

                unsigned long long div = rem / v0;
                rem -= div * v0;
                q[j] = (unsigned int)div;
            }
            r[0] = (unsigned int)rem;
        }
        else if (m >= n)
        {
            int shift = GetNormalizeShift(v[n - 1]);

            unsigned int un[4] = { 0 };
            unsigned int vn[4] = { 0 };

            Normalize(u, m, un, shift);
            Normalize(v, n, vn, shift);

            //  Main division loop
            //
            for (int j = m - n; j >= 0; j--)
            {
                unsigned long long rr, qq;
                int i;

                rr = Base32 * un[j + n] + un[j + n - 1];
                qq = rr / vn[n - 1];
                rr -= qq * vn[n - 1];

                for (;;)
                {
                    // Estimate too big ?
                    //
                    if ((qq >= Base32) || (qq * vn[n - 2] > (rr * Base32 + un[j + n - 2])))
                    {
                        qq--;
                        rr += vn[n - 1];
                        if (rr < Base32)
                        {
                            continue;
                        }
                    }
                    break;
                }

                //  Multiply and subtract
                //
                signed long long b = 0;
                signed long long t = 0;
                for (i = 0; i < n; i++)
                {
                    unsigned long long p = vn[i] * qq;
                    t = un[i + j] - (signed long long)(unsigned int)p - b;
                    un[i + j] = (unsigned int)t;
                    p >>= 32;
                    t >>= 32;
                    b = (signed long long)p - t;
                }
                t = un[j + n] - b;
                un[j + n] = (unsigned int)t;

                //  Store the calculated value
                //
                q[j] = (unsigned int)qq;

                //  Add back vn[0..n] to un[j..j+n]
                //
                if (t < 0)
                {
                    q[j]--;
                    unsigned long long c = 0;
                    for (i = 0; i < n; i++)
                    {
                        c = (unsigned long long)vn[i] + un[j + i] + c;
                        un[j + i] = (unsigned int)c;
                        c >>= 32;
                    }
                    c += un[j + n];
                    un[j + n] = (unsigned int)c;
                }
            }

            Unnormalize(un, r, shift);
        }
        else
        {
            memset(q, 0, 16);
            memcpy(r, u, 16);
        }
    }
}

namespace NxMath
{
    using NxInt16 = std::int16_t;
    using NxUInt16 = std::uint16_t;
    using NxInt32 = std::int32_t;
    using NxUInt32 = std::uint32_t;
    using NxInt64 = std::int64_t;
    using NxUInt64 = std::uint64_t;
    using NxInt128 = liulilittle::Int128;

    enum EContainmentType : int
    {
        Disjoint,
        Contains,
        Intersects
    };

	template <typename T>
	struct ElementType { using ResulType = T; };
	template<typename T, size_t N>
	struct ElementType <T[N]> : public ElementType<T> {};

	struct ArrayInfo
	{
		template<typename T>
		static constexpr int Length(const T& array)
		{
			return sizeof(array) / sizeof(ElementType<T>::ResulType);
		}
	};

    template<typename ValueType, int Count>
    struct SetBitMask
    {
        static const ValueType ResultValue = ((SetBitMask<ValueType, Count - 1>::ResultValue << 1) | 1);
    };
    template<typename ValueType>
    struct SetBitMask<ValueType, 0>
    {
        static const ValueType ResultValue = 0;
    };

    template <typename SignedType>
    struct UnsignedType
    {
        using ResultType = SignedType;
    };
    template <>
    struct UnsignedType<NxInt16>
    {
        using ResultType = NxUInt16;
    };
    template <>
    struct UnsignedType<NxInt32>
    {
        using ResultType = NxUInt32;
    };
    template <>
    struct UnsignedType<NxInt64>
    {
        using ResultType = NxUInt64;
    };

#if defined(__clang__)
    template<typename _ValueType, unsigned int _FracBit>
    struct NxConstValueByDouble
    {
        using ThisType = NxConstValueByDouble<_ValueType, _FracBit>;
        using UnsignedValueType = typename UnsignedType<_ValueType>::ResultType;
        static const UnsignedValueType FractionMask = SetBitMask<_ValueType, _FracBit>::ResultValue;
        static const UnsignedValueType Scalar = FractionMask + 1;
        static constexpr const _ValueType ResultValue(double v = 0)
        {
            return (_ValueType)(v * (double)Scalar);
        }
    };
    #define NxCValue(_Value,_ValueType,_FracBit) NxConstValueByDouble<_ValueType,_FracBit>::ResultValue(_Value)
#else
    template<double _Value, typename _ValueType, unsigned int _FracBit>
    struct NxConstValue
    {
        using ThisType = NxConstValue<_Value, _ValueType, _FracBit>;
        using UnsignedValueType = UnsignedType<_ValueType>::ResultType;
        static const UnsignedValueType FractionMask = SetBitMask<_ValueType, _FracBit>::ResultValue;
        static const UnsignedValueType Scalar = FractionMask + 1;
        //static const _ValueType ResultValue = (_ValueType)(_Value * (double)Scalar);
        static constexpr const _ValueType ResultValue(double v = 0)
        {
            PrintCode();
            return (_ValueType)(_Value * (double)Scalar);
        }

        inline static void PrintCode()
        {
#if _DEBUG
            static bool IsPrinted = false;
            if (IsPrinted)
            {
                return;
            }
            IsPrinted = true;

            std::string output;
            output += "Value = " + std::to_string(_Value) + ";";
            output += "FracBit = " + std::to_string(_FracBit) + ";";
            const auto fixedValue = (_ValueType)(_Value * (double)Scalar);
            output += "return " + std::to_string(fixedValue);
#endif
        }
    };
    #define NxCValue(_Value,_ValueType,_FracBit) NxConstValue<_Value,_ValueType,_FracBit>::ResultValue(_Value)
#endif
}

//后续通过工具生成代码，不同的ValueType, FracBit产生不同的整形立即数
#include "NxFixedConstValue.inl"
