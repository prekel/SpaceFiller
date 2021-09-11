[<AutoOpen>]
module SpaceFiller.Prelude

open System.Threading.Tasks
open FSharp.Control.Tasks
open Fabulous

let inline (^) a b = a b

[<RequiresExplicitTypeArguments>]
let inline ignore<'T> (a: 'T) = ignore a

module Fabulous =
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
