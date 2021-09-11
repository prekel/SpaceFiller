module SpaceFiller.FillerPage

open Fabulous
open Fabulous.XamarinForms

type Msg = unit

type Model = unit

let init () = (), Cmd.none

let update _msg model = model, Cmd.none

let view (_model: Model) _dispatch = View.ContentPage()
