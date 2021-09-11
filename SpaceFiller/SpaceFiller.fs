﻿namespace SpaceFiller

open System.Diagnostics
open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.LiveUpdate
open Xamarin.Forms

module App =
    type Model =
        { Count: int
          Step: int
          TimerOn: bool
          ShibePageModel: ShibePage.Model
          MapPageModel: MapPage.Model }

    type Msg =
        | Increment
        | Decrement
        | Reset
        | SetStep of int
        | TimerToggled of bool
        | TimedTick
        | ShibePageMsg of ShibePage.Msg
        | MapPageMsg of MapPage.Msg

    let init () =
        let initShibeModel, _ = ShibePage.init ()
        let initMapModel, _ = MapPage.init ()

        { Count = 0
          Step = 1
          TimerOn = false
          ShibePageModel = initShibeModel
          MapPageModel = initMapModel },
        Cmd.none

    let timerCmd =
        async {
            do! Async.Sleep 200
            return TimedTick
        }
        |> Cmd.ofAsyncMsg

    let update msg model =
        match msg with
        | Increment ->
            { model with
                  Count = model.Count + model.Step },
            Cmd.none
        | Decrement ->
            { model with
                  Count = model.Count - model.Step },
            Cmd.none
        | Reset -> init ()
        | SetStep n -> { model with Step = n }, Cmd.none
        | TimerToggled on -> { model with TimerOn = on }, (if on then timerCmd else Cmd.none)
        | TimedTick ->
            if model.TimerOn then
                { model with
                      Count = model.Count + model.Step },
                timerCmd
            else
                model, Cmd.none
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
                [ View.ContentPage(
                    content =
                        View.StackLayout(
                            padding = Thickness 20.0,
                            verticalOptions = LayoutOptions.Center,
                            children =
                                [ View.Label(
                                    text = sprintf "%d" model.Count,
                                    horizontalOptions = LayoutOptions.Center,
                                    width = 200.0,
                                    horizontalTextAlignment = TextAlignment.Center
                                  )
                                  View.Button(
                                      text = "Increment",
                                      command = (fun () -> dispatch Increment),
                                      horizontalOptions = LayoutOptions.Center
                                  )
                                  View.Button(
                                      text = "Decrement",
                                      command = (fun () -> dispatch Decrement),
                                      horizontalOptions = LayoutOptions.Center
                                  ) ]
                        )
                  )
                  ShibePage.view model.ShibePageModel (dispatch << ShibePageMsg)
                  MapPage.view model.MapPageModel (dispatch << MapPageMsg) ]
        )

    // Note, this declaration is needed if you enable LiveUpdate
    let program =
        XamarinFormsProgram.mkProgram init update view
#if DEBUG
        |> Program.withConsoleTrace
#endif

type App() as app =
    inherit Application()

    let runner =
        App.program |> XamarinFormsProgram.run app

#if DEBUG
// Uncomment this line to enable live update in debug mode.
// See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/tools.html#live-update for further  instructions.
//
//do runner.EnableLiveUpdate()
#endif

// Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
// See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/models.html#saving-application-state for further  instructions.
#if APPSAVE
    let modelId = "model"

    override __.OnSleep() =

        let json =
            Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)

        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() =
        Console.WriteLine "OnResume: checking for model in app.Properties"

        try
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) ->

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)

                let model =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel(model, Cmd.none)

            | _ -> ()
        with
        | ex -> App.program.onError ("Error while restoring model found in app.Properties", ex)

    override this.OnStart() =
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif
