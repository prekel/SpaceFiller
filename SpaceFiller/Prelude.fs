[<AutoOpen>]
module SpaceFiller.Prelude

let inline (^) a b = a b

[<RequiresExplicitTypeArguments>]
let inline ignore<'T> (a: 'T) = ignore a

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

        let ofTaskMsgOption (p: unit -> Task<'msg option>) : Cmd<'msg> =
            [ fun dispatch ->
                  unitTask {
                      let! msg = p ()

                      match msg with
                      | None -> ()
                      | Some msg -> dispatch msg
                  }
                  |> ignore<Task> ]
