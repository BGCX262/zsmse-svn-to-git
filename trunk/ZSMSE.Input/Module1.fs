(*
    The code in this module might be poor
    as I don't actually know F# very well yet.

    I'm sure there's better more functional ways of doing things.
    I still have my C hat on.
*)

namespace Zoofware.ZSMSE

open System
open Zoofware.ZZ80E

type Input() = class
    (* I/O port A *)
    let AB : bool array = Array.zeroCreate 7

    (* I/O port B *)
    let BMisc : bool array = Array.zeroCreate 7

    let boolsToByte(array : bool[]) : byte =
        let mutable acc = 0xFFuy

        (*
            Internally, the SMS sees an off bit as showing the
            relavent button is pressed.

            Likewise a true bit means the button is not pressed.

            This is why the values are reversed below.
        *)

        for i = 0 to array.Length - 1 do
            match array.[i] with
            | false -> (acc <- StaticHelpers.SetBit(acc, i, true))
            | true -> (acc <- StaticHelpers.SetBit(acc, i, false))          

        acc

    /// <summary>
    /// Returns the A/B register.
    /// </summary>
    member this.RegisterAB : byte = 
        boolsToByte AB

    /// <summary>
    /// Returns the B/Misc register.
    /// </summary>
    member this.RegisterBMisc : byte = 
        boolsToByte BMisc

    (* Port A/B *)
    member this.P1Up
        with get() = AB.[0]
        and set(value : bool) = AB.[0] <- value

    member this.P1Down
        with get() = AB.[1]
        and set(value : bool) = AB.[1] <- value

    member this.P1Left
        with get() = AB.[2]
        and set(value : bool) = AB.[2] <- value

    member this.P1Right
        with get() = AB.[3]
        and set(value : bool) = AB.[3] <- value

    member this.P1FireA
        with get() = AB.[4]
        and set(value : bool) = AB.[4] <- value

    member this.P1FireB
        with get() = AB.[5]
        and set(value : bool) = AB.[5] <- value
    
    member this.P2Up
        with get() = AB.[6]
        and set(value : bool) = AB.[6] <- value

    member this.P2Down
        with get() = AB.[7]
        and set(value : bool) = AB.[7] <- value

    (* Port B/Misc *)
    member this.P2Left
        with get() = BMisc.[0]
        and set(value : bool) = BMisc.[0] <- value

    member this.P2Right
        with get() = BMisc.[1]
        and set(value : bool) = BMisc.[1] <- value

    member this.P2FireA
        with get() = BMisc.[2]
        and set(value : bool) = BMisc.[2] <- value

    member this.P2FireB
        with get() = BMisc.[3]
        and set(value : bool) = BMisc.[3] <- value

    member this.Reset
        with get() = BMisc.[4]
        and set(value : bool) = BMisc.[4] <- value

    (* bit 5 is unused *)

    member this.P1Th
        with get() = BMisc.[6]
        and set(value : bool) = BMisc.[6] <- value

    member this.P2Th
        with get() = BMisc.[7]
        and set(value : bool) = BMisc.[7] <- value
end