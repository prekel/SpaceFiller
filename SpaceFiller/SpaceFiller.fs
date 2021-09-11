﻿namespace SpaceFiller

open System
open Fabulous
open Fabulous.XamarinForms
open Xamarin.Forms

module App =
    type Model =
        { FillerPageModel: FillerPage.Model
          ShibePageModel: ShibePage.Model
          MapPageModel: MapPage.Model }

    type Msg =
        | FillerPageMsg of FillerPage.Msg
        | ShibePageMsg of ShibePage.Msg
        | MapPageMsg of MapPage.Msg

    let init () =
        let initFillerModel, _ = FillerPage.init ()
        let initShibeModel, _ = ShibePage.init ()
        let initMapModel, _ = MapPage.init ()

        { FillerPageModel = initFillerModel
          ShibePageModel = initShibeModel
          MapPageModel = initMapModel },
        Cmd.none

    let update msg model =
        match msg with
        | FillerPageMsg msg ->
            let fillerPageModel, fillerPageCmd =
                FillerPage.update msg model.FillerPageModel

            { model with
                  FillerPageModel = fillerPageModel },
            fillerPageCmd |> Cmd.map ShibePageMsg
        | ShibePageMsg msg ->
            let shibePageModel, shibePageCmd =
                ShibePage.update msg model.ShibePageModel

            { model with
                  ShibePageModel = shibePageModel },
            shibePageCmd |> Cmd.map ShibePageMsg
        | MapPageMsg msg ->
            let mapPageModel, mapPageCmd = MapPage.update msg model.MapPageModel

            { model with
                  MapPageModel = mapPageModel },
            mapPageCmd |> Cmd.map ShibePageMsg

    let view (model: Model) dispatch =
        View.TabbedPage(
            children =
                [ FillerPage.view model.FillerPageModel (dispatch << FillerPageMsg)
                  ShibePage.view model.ShibePageModel (dispatch << ShibePageMsg)
                  MapPage.view model.MapPageModel (dispatch << MapPageMsg) ]
        )

    let program =
        XamarinFormsProgram.mkProgram init update view
#if DEBUG
        |> Program.withConsoleTrace
#endif

type App() as app =
    inherit Application()

    let _ = App.program |> XamarinFormsProgram.run app

    override _.OnSleep() = Console.WriteLine "OnSleep"

    override _.OnResume() = Console.WriteLine "OnResume"

    override _.OnStart() = Console.WriteLine "OnStart"
