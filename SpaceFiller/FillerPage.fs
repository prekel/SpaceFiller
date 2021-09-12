module SpaceFiller.FillerPage

open FSharp.Control.Tasks.NonAffine
open Fabulous
open Fabulous.XamarinForms
open Xamarin.Forms
open SqlHydra.Query
open SpaceFiller.Glyphs
open SpaceFiller.Db
open SpaceFiller.Tables

type Msg =
    | Abc of int option list
    | More
    | More1

type Model = { Ab: int option list }

let get1 (ctx: QueryContext) () =
    task {
        let! a =
            select {
                for p in table_name do
                    select p.column_1
            }
            |> ctx.ReadAsync HydraReader.Read

        return a |> Seq.toList |> Abc
    }

let init (ctx: QueryContext) () = { Ab = [] }, Cmd.ofTaskMsg (get1 ctx)

let update (ctx: QueryContext) msg model =
    match msg with
    | Abc a -> { model with Ab = a }, Cmd.none
    | More -> { model with Ab = [] }, Cmd.ofTaskMsg (get1 ctx)
    | More1 ->
        let a =
            select {
                for p in table_name do
                    select p.column_1
            }
            |> ctx.Read HydraReader.Read

        { model with Ab = a |> Seq.toList }, Cmd.none

let view model dispatch =
    View.ContentPage(
        title = "Filler",
        icon = Image.fromFont (FontImageSource(Glyph = FA.History, FontFamily = "FA")),
        content =
            View.StackLayout(
                padding = Thickness 20.0,
                children =
                    [ View.Label(text = (model.Ab |> string))
                      View.Button(text = "More", command = (fun () -> dispatch More1)) ]
            )
    )
