module SpaceFiller.MapPage

open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.Maps
open SpaceFiller.Glyphs
open Xamarin.Forms.Maps

type Msg = ChangeMapType of MapType

type Model = { MapType: MapType }

let init () = { MapType = MapType.Street }, Cmd.none

let update msg model =
    match msg with
    | ChangeMapType mt -> { model with MapType = mt }, Cmd.none

let mapRef =
    ViewRef<Map>()
    >!!=> fun map -> map.TrafficEnabled <- true

let view model dispatch =
    View.ContentPage(
        title = "Map",
        icon = Image.icon (Fa FA.Map),
        content =
            View.Map(
                ref = mapRef,
                mapType = model.MapType,
                pins =
                    [ View.Pin(
                          address = "г.Красноярск, ул.Академика Киренского 26 к/1",
                          label = "ИКИТ",
                          position = Position(55.994337, 92.797489)
                      ) ],
                mapClicked =
                    (fun _ ->
                        match model.MapType with
                        | MapType.Street -> MapType.Satellite
                        | MapType.Satellite -> MapType.Hybrid
                        | _ -> MapType.Street
                        |> ChangeMapType
                        |> dispatch),
                requestedRegion = MapSpan.FromCenterAndRadius(Position(56.0043669, 92.8026209), Distance(1000.)),
                isShowingUser = true
            )
    )
