module SpaceFiller.ShibePage

open System.Net.Http
open FSharp.Control.Tasks.NonAffine
open Fabulous
open Fabulous.XamarinForms
open Newtonsoft.Json
open SpaceFiller.Glyphs
open Xamarin.Forms

type Msg =
    | SetRequested of int
    | Show
    | ImageListReceived of string list
    | ImageDownloaded of byte array

type Model =
    { RequestedImagesCount: int
      ImagesViews: ViewElement list }

let downloadUrs count () =
    task {
        let url =
            $"http://shibe.online/api/shibes?count=%d{count}"

        use httpClient = new HttpClient()
        let! a = httpClient.GetAsync(url)
        let! content = a.Content.ReadAsStringAsync()

        let response =
            JsonConvert.DeserializeObject<_>(content)

        return response |> List.ofArray |> ImageListReceived
    }

let downloadImage (path: string) () =
    task {
        use httpClient = new HttpClient()
        let! a = httpClient.GetAsync(path)
        let! content = a.Content.ReadAsByteArrayAsync()
        return ImageDownloaded content
    }

let init () =
    { RequestedImagesCount = 1
      ImagesViews = [] },
    Cmd.ofMsg Show

let update msg model =
    match msg with
    | SetRequested cnt ->
        { model with
              RequestedImagesCount = cnt },
        Cmd.none
    | Show -> { model with ImagesViews = [] }, Cmd.ofTaskMsg (downloadUrs model.RequestedImagesCount)
    | ImageListReceived images ->
        model,
        images
        |> List.map (fun path -> Cmd.ofTaskMsg (downloadImage path))
        |> Cmd.batch
    | ImageDownloaded image ->
        { model with
              ImagesViews =
                  View.Image(source = Image.fromBytes image)
                  :: model.ImagesViews },
        Cmd.none

let view (model: Model) dispatch =
    View.ContentPage(
        title = "Shibe",
        icon = Image.icon (Fa FA.Dog),
        content =
            View.StackLayout(
                padding = Thickness 20.0,
                children =
                    [ View.Slider(
                        minimumMaximum = (float 1, float 50),
                        value = float 1,
                        valueChanged =
                            (fun value ->
                                value.NewValue
                                |> (+) 0.5
                                |> int
                                |> SetRequested
                                |> dispatch)
                      )
                      View.Button(
                          horizontalOptions = LayoutOptions.Center,
                          text = $"Show {model.RequestedImagesCount} images",
                          command = (fun () -> dispatch Show),
                          commandCanExecute =
                              (model.RequestedImagesCount
                               <> (model.ImagesViews |> List.length))
                      )
                      View.ScrollView(
                          verticalOptions = LayoutOptions.Fill,
                          content = View.StackLayout(children = model.ImagesViews)
                      ) ]
            )
    )
