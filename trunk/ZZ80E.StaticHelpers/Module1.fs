namespace Zoofware.ZZ80E

type StaticHelpers = class
    (*
        Methods won't be inlined when called from C#
        but will be when they're used from F#
    *)

    static member inline TestBit (bitset : int32, position : int) : bool =
        (((bitset >>> position)) &&& 1) = 1

    static member inline SetBit (bitset : byte, position : int, value : bool) : byte =
        if StaticHelpers.TestBit((int32) bitset, position) = value then
            bitset
        else
            bitset ^^^ (1uy <<< position)

    static member inline SetBit (bitset : uint16, position : int, value : bool) : uint16 =
        if StaticHelpers.TestBit((int32) bitset, position) = value then
            bitset
        else
            bitset ^^^ (1us <<< position)

    static member inline SetBit (bitset : int32, position : int, value : bool) : int32 =
        if StaticHelpers.TestBit((int32) bitset, position) = value then
            bitset
        else
            bitset ^^^ (1 <<< position)

    /// <summary>
    /// Tests bit parity of a byte
    /// </summary>
    /// <returns>True if the number of 1s in a byte is even.</returns>
    static member inline TestParity (bitset : byte) : bool =
        let mutable trueCount = 0
        let mutable value = bitset

        while value > 0uy do
            if (StaticHelpers.TestBit((int32) value, 0)) = true then
                trueCount <- trueCount + 1

            value <- value >>> 1

        if trueCount % 2 = 0 then
            true
        else
            false

    static member inline TestHalfCarry (x : byte, y : byte, result : byte) : bool =
        ((x ^^^ y ^^^ result) &&& 0x10uy) <> 0uy

    /// <summary>Tests for half carry over the lower *11 bits* of each ushort.</summary>
    static member inline TestHalfCarry (x : uint16, y : uint16, result : uint16) : bool =
        ((x ^^^ y ^^^ result) &&& 0x1000us) <> 0us
    
    static member inline TestAdditionOverflow (x : byte, y : byte, result : byte) : bool =
        ((y ^^^ x ^^^ 0x80uy) &&& (x ^^^ result) &&& 0x80uy) <> 0uy

    static member inline TestAdditionOverflow (x : uint16, y : uint16, result : uint16) : bool =
        ((y ^^^ x ^^^ 0x8000us) &&& (x ^^^ result) &&& 0x8000us) <> 0us

    /// <summary>Tests the signed overflow of a 16 bit subtraction.</summary> 
    /// <param name="x">Minuend.</param>
    /// <param name="y">Subtrahend.</param>
    /// <param name="result">Result of the actual equation.</param>
    static member inline TestSubtractionOverflow (x : uint16, y : uint16, result : uint16) : bool =
        ((y ^^^ x) &&& (x ^^^ result) &&& 0x8000us) <> 0us

    /// <summary>Tests the signed overflow of an 8 bit subtraction.</summary> 
    /// <param name="x">Minuend.</param>
    /// <param name="y">Subtrahend.</param>
    /// <param name="result">Result of the actual equation.</param>
    static member inline TestSubtractionOverflow (x : byte, y : byte, result : byte) : bool =
        ((y ^^^ x) &&& (x ^^^ result) &&& 0x80uy) <> 0uy
end
