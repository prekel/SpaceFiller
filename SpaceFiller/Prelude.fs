[<AutoOpen>]
module SpaceFiller.Prelude

let inline (^) a b = a b

let inline (^/) a b = System.IO.Path.Combine(a, b)

[<RequiresExplicitTypeArguments>]
let inline ignore<'T> (a: 'T) = ignore a

let inline undefined<'T> : 'T = raise ^ System.NotImplementedException()

[<RequiresExplicitTypeArguments>]
let inline unreachable<'T> : 'T =
    raise ^ System.InvalidOperationException()

[<Measure>]
type mB

let (|Bytes|) (megaBytes: int<mB>) =
    megaBytes |> int64 |> (*) 1024L |> (*) 1024L

let (|MegaBytes|) (bytes: int64) : int<mB> =
    (decimal bytes) / 1024m / 1024m
    |> int
    |> (*) 1<mB>

module Fabulous =
    open System.Threading.Tasks
    open FSharp.Control.Tasks.NonAffine
    open Fabulous

    let (>!!=>) (a: ViewRef<_>) b =
        a.Attached.Add b
        a

    module Cmd =
        let ofTaskMsg (p: unit -> Task<'msg>) : Cmd<'msg> =
            [ fun dispatch ->
                  unitTask {
                      let! msg = p ()
                      dispatch msg
                  }
                  |> ignore<Task> ]

        let ofTaskMsgErr (p: unit -> Task<'msg>) (toErr: exn -> 'msg) : Cmd<'msg> =
            [ fun dispatch ->
                  unitTask {
                      let! msg =
                          try
                              p ()
                          with
                          | ex -> Task.FromResult(toErr ex)

                      dispatch msg
                  }
                  |> ignore<Task> ]

        let ofTaskMsgOption (p: unit -> Task<'msg option>) : Cmd<'msg> =
            [ fun dispatch ->
                  unitTask {
                      let! msg = p ()

                      match msg with
                      | None -> ()
                      | Some msg -> dispatch msg
                  }
                  |> ignore<Task> ]

    type Icon = Fa of string

    module XamarinForms =
        module Image =
            open Fabulous.XamarinForms.InputTypes
            open Xamarin.Forms

            let icon (Fa glyph) =
                Image.fromFont (FontImageSource(Glyph = glyph, FontFamily = "FA"))
