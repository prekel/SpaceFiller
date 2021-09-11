module SpaceFiller.MapPage

open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.Maps
open SpaceFiller.Glyphs
open Xamarin.Forms
open Xamarin.Forms.Maps

type Msg = ChangeMapType of MapType

type Model = { MapType: MapType }

let init () = { MapType = MapType.Street }, Cmd.none

let update msg model =
    match msg with
    | ChangeMapType mt -> { model with MapType = mt }, Cmd.none

let rec mapRef = ViewRef<Map>()
mapRef.Attached.Add(fun map -> map.TrafficEnabled <- true)

let view (model: Model) dispatch =
    View.ContentPage(
        title = "Map",
        icon = Image.fromFont (FontImageSource(Glyph = FA.Map, FontFamily = "FA")),
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
