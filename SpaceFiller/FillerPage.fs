module SpaceFiller.FillerPage

open System
open FSharp.Control.Tasks.NonAffine
open Fabulous
open Fabulous.XamarinForms
open Xamarin.Forms
open SqlHydra.Query
open SpaceFiller.Glyphs
open SpaceFiller.Db
open SpaceFiller.Tables

type Request =
    { Requested: int<mB>
      Precision: int<mB>
      Keep: int<mB> }

[<RequireQualifiedAccess>]
type Operation =
    | Refresh
    | Set
    | Reset

type FillRecord =
    { Id: int64
      Operation: Operation
      Request: Request option
      Filled: int<mB>
      Free: int<mB>
      DateTime: DateTimeOffset option }
    static member FromDb(dbo: main.fill_record) =
        { Id = dbo.id
          Operation =
              match dbo.operation with
              | 1 -> Operation.Refresh
              | 2 -> Operation.Set
              | 3 -> Operation.Reset
              | _ -> unreached
          Request =
              match dbo.requested, dbo.precision, dbo.keep with
              | Some requested, Some precision, Some keep ->
                  { Requested = requested * 1<mB>
                    Precision = precision * 1<mB>
                    Keep = keep * 1<mB> }
                  |> Some
              | _ -> None
          Filled = dbo.filled * 1<mB>
          Free = dbo.free * 1<mB>
          DateTime = dbo.date_time |> Option.map DateTimeOffset.Parse }

    member this.ToDb() =
        { main.fill_record.id = this.Id
          main.fill_record.operation =
              match this.Operation with
              | Operation.Refresh -> 1
              | Operation.Set -> 2
              | Operation.Reset -> 3
          main.fill_record.requested =
              this.Request
              |> Option.map (fun r -> int r.Requested)
          main.fill_record.precision =
              this.Request
              |> Option.map (fun r -> int r.Precision)
          main.fill_record.keep = this.Request |> Option.map (fun r -> int r.Keep)
          main.fill_record.filled = int this.Filled
          main.fill_record.free = int this.Free
          main.fill_record.date_time =
              this.DateTime
              |> Option.map (fun dt -> dt.ToString("o")) }

type Msg =
    | Refresh
    | Set of Request
    | Reset
    | Load
    | Loaded of FillRecord list
    | Err of exn

type Model =
    { Records: FillRecord list
      Err: string option }

let load (ctx: QueryContext) () =
    task {
        let! fr =
            select {
                for r in fill_record do
                    select r
            }
            |> ctx.ReadAsync HydraReader.Read

        return
            fr
            |> Seq.map FillRecord.FromDb
            |> List.ofSeq
            |> Loaded
    }

let init (ctx: QueryContext) () = { Records = []; Err = None }, Cmd.none

let update (ctx: QueryContext) msg model =
    match msg with
    | Refresh -> undefined
    | Set _ -> undefined
    | Reset -> undefined
    | Load -> model, Cmd.ofTaskMsgErr (load ctx) Err
    | Loaded records -> { model with Records = records }, Cmd.none
    | Err ex ->
        { model with
              Err = ex |> string |> Some },
        Cmd.none

let viewTable model =
    View.ScrollView(
        content =
            View.Grid(
                coldefs =
                    [ Dimension.Star
                      Dimension.Star
                      Dimension.Star
                      Dimension.Star
                      Dimension.Star ],
                rowdefs =
                    (model.Records
                     |> List.map (fun a -> Dimension.Absolute 50.)),
                children = [ //for i in model.Records do
                                  ]
            )
    )

let view model dispatch =
    View.ContentPage(
        title = "Filler",
        icon = Image.fromFont (FontImageSource(Glyph = FA.History, FontFamily = "FA")),
        content =
            View.Grid(
                padding = Thickness 20.0,
                rowdefs = [ Dimension.Star; Dimension.Auto ],
                coldefs =
                    [ Dimension.Stars 3.
                      Dimension.Stars 1. ],
                children =
                    [ (viewTable model).Row(0).ColumnSpan(2)
                      View
                          .ListView(
                              items =
                                  [ View.EntryCell()
                                    View.EntryCell()
                                    View.EntryCell() ]
                          )
                          .Row(1)
                          .Column(0)
                      View
                          .StackLayout(
                              children =
                                  [ View.Button(text = "Refresh", command = (fun () -> dispatch Refresh))
                                    View.Button(text = "More", command = (fun () -> dispatch Load))
                                    View.Button(text = "Reset", command = (fun () -> dispatch Reset)) ]
                          )
                          .Row(1)
                          .Column(1) ]
            )
    )
