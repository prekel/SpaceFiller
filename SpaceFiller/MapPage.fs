module SpaceFiller.MapPage

open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.Maps
open SpaceFiller.Glyphs
open Xamarin.Forms

type Msg = unit

type Model = unit

let init () = (), Cmd.none

let update _msg model = model, Cmd.none

let view (_model: Model) _dispatch =
    View.ContentPage(
        title = "Map",
        icon = Image.fromFont (FontImageSource(Glyph = FA.Map, FontFamily = "FA")),
        content = View.Map()
    )
