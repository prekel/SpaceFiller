namespace SpaceFiller

open System
open System.IO
open Fabulous
open Fabulous.XamarinForms
open Xamarin.Forms.PlatformConfiguration
open Xamarin.Forms.PlatformConfiguration.AndroidSpecific
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

    let init queryContext () =
        let initFillerModel, initFillerCmd = FillerPage.init queryContext ()
        let initShibeModel, initShibeCmd = ShibePage.init ()
        let initMapModel, initMapCmd = MapPage.init ()

        { FillerPageModel = initFillerModel
          ShibePageModel = initShibeModel
          MapPageModel = initMapModel },
        Cmd.batch [ initFillerCmd |> Cmd.map FillerPageMsg
                    initShibeCmd |> Cmd.map ShibePageMsg
                    initMapCmd |> Cmd.map MapPageMsg ]

    let tabbedPageRef =
        ViewRef<TabbedPage>()
        >!!=> fun tabbedPage ->
                  tabbedPage
                      .On<Android>()
                      .SetToolbarPlacement(ToolbarPlacement.Bottom)
                  |> ignore<IPlatformElementConfiguration<Android, TabbedPage>>

    let update queryContext msg model =
        match msg with
        | FillerPageMsg msg ->
            let fillerPageModel, fillerPageCmd =
                FillerPage.update queryContext msg model.FillerPageModel

            { model with
                  FillerPageModel = fillerPageModel },
            fillerPageCmd |> Cmd.map FillerPageMsg
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
            mapPageCmd |> Cmd.map MapPageMsg

    let view (model: Model) dispatch =
        View.TabbedPage(
            ref = tabbedPageRef,
            children =
                [ FillerPage.view model.FillerPageModel (dispatch << FillerPageMsg)
                  ShibePage.view model.ShibePageModel (dispatch << ShibePageMsg)
                  MapPage.view model.MapPageModel (dispatch << MapPageMsg) ]
        )

    let program queryContext =
        XamarinFormsProgram.mkProgram (init queryContext) (update queryContext) view
#if DEBUG
        |> Program.withConsoleTrace
#endif

open SqlHydra.Query
open Microsoft.Data.Sqlite
open SqlKata.Compilers

type App() as app =
    inherit Application()

    do
        if not ^ Directory.Exists(Global.appDataPath) then
            Directory.CreateDirectory(Global.appDataPath)
            |> ignore<DirectoryInfo>

    let queryContext () =
        let compiler = SqliteCompiler()

        if File.Exists(Global.dbPath) |> not then
        //if true then
            let assembly = typeof<App>.Assembly

            use stream =
                assembly.GetManifestResourceStream($"{Global.appName}.%s{Global.dbName}")

            use fs =
                new FileStream(Global.dbPath, FileMode.OpenOrCreate)

            stream.CopyTo(fs)

        let conn =
            new SqliteConnection(Global.connectionString)

        conn.Open()

        new QueryContext(conn, compiler)

    do
        App.program (queryContext ())
        |> XamarinFormsProgram.run app
        |> ignore<ProgramRunner<unit, App.Model, App.Msg>>
