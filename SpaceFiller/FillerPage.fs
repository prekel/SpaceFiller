module SpaceFiller.FillerPage

open System
open System.IO
open FSharp.Control
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

module FillRecord =
    let fromDb (dbo: main.fill_record) =
        { Id = dbo.id
          Operation =
              match dbo.operation with
              | 1 -> Operation.Refresh
              | 2 -> Operation.Set
              | 3 -> Operation.Reset
              | _ -> unreachable<Operation>
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

    let toDb this =
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
    | Load of int64
    | Loaded of FillRecord list
    | UpdateRequestedEntry of string
    | UpdatePrecisionEntry of string
    | UpdateKeepEntry of string
    | ShowMessage of string
    | Err of exn

type Model =
    { Records: FillRecord list
      Err: string option
      RequestedEntry: string
      PrecisionEntry: string
      KeepEntry: string }

let requestFromModel model =
    match model.RequestedEntry |> Int32.TryParse,
          model.PrecisionEntry |> Int32.TryParse,
          model.KeepEntry |> Int32.TryParse with
    | (true, requested), (true, precision), (true, keep) ->
        { Requested = requested * 1<mB>
          Precision = precision * 1<mB>
          Keep = keep * 1<mB> }
        |> Some
    | _ -> None

let load (ctx: QueryContext) minId () =
    task {
        let! fr =
            select {
                for r in fill_record do
                    where (r.id >= minId)
                    orderByDescending r.id
                    select r
            }
            |> ctx.ReadAsync HydraReader.Read

        return
            fr
            |> Seq.map FillRecord.fromDb
            |> List.ofSeq
            |> Loaded
    }

let insertRecord (ctx: QueryContext) rcd () =
    task {
        let dbo = rcd |> FillRecord.toDb

        let! (id: int64) =
            insert {
                for d in fill_record do
                    entity dbo
                    excludeColumn d.id
            }
            |> ctx.InsertAsync

        return Load id
    }

let fillDataPath = Global.appDataPath ^/ "fill"

let createFilledDir () =
    if not ^ Directory.Exists fillDataPath then
        Directory.CreateDirectory fillDataPath
        |> ignore<DirectoryInfo>

let getFreeSpace () =
    DependencyService
        .Get<Services.IFreeStorage>()
        .GetFreeStorage()
    |> function
        | MegaBytes a -> a

let getFilled () =
    DirectoryInfo(fillDataPath).EnumerateFiles()
    |> Seq.map (fun file -> file.Length)
    |> Seq.sum
    |> (function
    | MegaBytes a -> a)

let setFilled req () =
    task {
        let free = getFreeSpace ()

        let (Bytes toFillAllBytes) = min req.Requested (free - req.Keep)
        let (Bytes precisionBytes) = req.Precision
        let chunks = toFillAllBytes / precisionBytes |> int
        let bytes = Array.zeroCreate (int precisionBytes)

        for i in 1 .. chunks do
            use fstream =
                new FileStream(fillDataPath ^/ $"%d{i}.bin", FileMode.OpenOrCreate)

            do! fstream.WriteAsync(bytes, 0, bytes.Length)
    }

let resetFilled () =
    DirectoryInfo(fillDataPath).EnumerateFiles()
    |> Seq.iter (fun file -> file.Delete())

let init (_ctx: QueryContext) () =
    createFilledDir ()

    { Records = []
      Err = None
      RequestedEntry = 1024<mB> |> string
      PrecisionEntry = 16<mB> |> string
      KeepEntry = 100<mB> |> string },
    Cmd.ofMsg (Load 0L)

let refreshCommand (ctx: QueryContext) model () =
    task {
        let filled = getFilled ()
        let free = getFreeSpace ()

        let rcd =
            { Id = 0L
              Operation = Operation.Refresh
              Request =
                  model.Records
                  |> List.tryHead
                  |> Option.bind (fun a -> a.Request)
              Filled = filled
              Free = free
              DateTime = DateTimeOffset.Now |> Some }

        return! insertRecord ctx rcd ()
    }

let setCommand (ctx: QueryContext) (req: Request) () =
    task {
        resetFilled ()
        do! setFilled req ()

        let free = getFreeSpace ()
        let filled = getFilled ()

        let rcd =
            { Id = 0L
              Operation = Operation.Set
              Request = Some req
              Filled = filled
              Free = free
              DateTime = DateTimeOffset.Now |> Some }

        return! insertRecord ctx rcd ()
    }

let resetCommand (ctx: QueryContext) () =
    task {
        resetFilled ()
        let filled = getFilled ()

        let free = getFreeSpace ()

        let rcd =
            { Id = 0L
              Operation = Operation.Reset
              Request = None
              Filled = filled
              Free = free
              DateTime = DateTimeOffset.Now |> Some }

        return! insertRecord ctx rcd ()
    }

let update (ctx: QueryContext) msg model =
    match msg with
    | Refresh -> model, Cmd.ofTaskMsgErr (refreshCommand ctx model) Err
    | Set req -> model, Cmd.ofTaskMsgErr (setCommand ctx req) Err
    | Reset -> model, Cmd.ofTaskMsgErr (resetCommand ctx) Err
    | Load minId -> model, Cmd.ofTaskMsgErr (load ctx minId) Err
    | Loaded records ->
        { model with
              Records = records @ model.Records },
        Cmd.none
    | UpdateRequestedEntry entry -> { model with RequestedEntry = entry }, Cmd.none
    | UpdatePrecisionEntry entry -> { model with PrecisionEntry = entry }, Cmd.none
    | UpdateKeepEntry entry -> { model with KeepEntry = entry }, Cmd.none
    | ShowMessage _msg -> model, Cmd.none
    | Err ex ->
        { model with
              Err = ex |> string |> Some },
        Cmd.none

let viewTable model =
    View.Grid(
        rowdefs = [ Dimension.Auto; Dimension.Star ],
        children =
            [ View
                .Grid(
                    coldefs =
                        [ Dimension.Stars 1.
                          Dimension.Stars 2.
                          Dimension.Stars 2.
                          Dimension.Stars 1.
                          Dimension.Stars 1.
                          Dimension.Stars 3. ],
                    children =
                        [ View
                            .Label(text = "Id", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(0)
                          View
                              .Label(text = "Operation", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(1)
                          View
                              .Label(text = "Requested, Precision, Keep, MB", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(2)
                          View
                              .Label(text = "Filled, MB", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(3)
                          View
                              .Label(text = "Free, MB", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(4)
                          View
                              .Label(text = "DateTime", lineBreakMode = LineBreakMode.WordWrap)
                              .Column(5) ]
                )
                  .Row(0)
              View
                  .ScrollView(
                      content =
                          View.StackLayout(
                              children =
                                  [ for i in model.Records do
                                        View.Grid(
                                            coldefs =
                                                [ Dimension.Stars 1.
                                                  Dimension.Stars 2.
                                                  Dimension.Stars 2.
                                                  Dimension.Stars 1.
                                                  Dimension.Stars 1.
                                                  Dimension.Stars 3. ],
                                            children =
                                                [ View.Label(text = (string i.Id)).Column(0)
                                                  View.Label(text = (string i.Operation)).Column(1)
                                                  View
                                                      .Label(
                                                          text =
                                                              (match i.Request with
                                                               | Some r -> $"%d{r.Requested}, {r.Precision}, {r.Keep}"
                                                               | None -> "-")
                                                      )
                                                      .Column(2)
                                                  View.Label(text = (string i.Filled)).Column(3)
                                                  View.Label(text = (string i.Free)).Column(4)
                                                  View
                                                      .Label(
                                                          text =
                                                              (match i.DateTime with
                                                               | Some dt -> string dt
                                                               | None -> "-")
                                                      )
                                                      .Column(5) ]
                                        ) ]
                          )
                  )
                  .Row(1) ]
    )

let view model dispatch =
    View.ContentPage(
        title = "Filler",
        icon = Image.icon (Fa FA.History),
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
                          .StackLayout(
                              children =
                                  [ View.Label(text = "Requested, MB")
                                    View.Entry(
                                        text = model.RequestedEntry,
                                        placeholder = "1024",
                                        textChanged = (fun a -> a.NewTextValue |> UpdateRequestedEntry |> dispatch)
                                    )
                                    View.Label(text = "Precision, MB")
                                    View.Entry(
                                        text = model.PrecisionEntry,
                                        placeholder = "16",
                                        textChanged = (fun a -> a.NewTextValue |> UpdatePrecisionEntry |> dispatch)
                                    )
                                    View.Label(text = "Keep, MB")
                                    View.Entry(
                                        text = model.KeepEntry,
                                        placeholder = "100",
                                        textChanged = (fun a -> a.NewTextValue |> UpdateKeepEntry |> dispatch)
                                    ) ]
                          )
                          .Row(1)
                          .Column(0)
                      View
                          .StackLayout(
                              verticalOptions = LayoutOptions.Center,
                              children =
                                  [ View.Button(text = "Refresh", command = (fun () -> dispatch Refresh))
                                    View.Button(
                                        text = "Set",
                                        command =
                                            (fun () ->
                                                match requestFromModel model with
                                                | Some req -> req |> Set |> dispatch
                                                | None -> "Must be numbers" |> ShowMessage |> dispatch)
                                    )
                                    View.Button(text = "Reset", command = (fun () -> dispatch Reset)) ]
                          )
                          .Row(1)
                          .Column(1) ]
            )
    )
