module SpaceFiller.MapPage

open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.Maps

type Msg = unit

type Model = unit

let init () = (), Cmd.none

let update _msg model = model, Cmd.none

let view (_model: Model) _dispatch = View.ContentPage(content = View.Map())
