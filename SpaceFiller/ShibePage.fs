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
    | ImageListReceived of string list
    | Show
    | ImageDownloaded of byte array

type Model =
    { RequestedImagesCount: int
      Images: byte array list }

let downloadUrs count () =
    task {
        let url =
            $"http://shibe.online/api/shibes?count={count}"

        use httpClient = new HttpClient()
        let! a = httpClient.GetAsync(url)
        let! content = a.Content.ReadAsStringAsync()

        let response =
            JsonConvert.DeserializeObject<string array>(content)

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
      Images = [] },
    Cmd.ofMsg Show

let update msg model =
    match msg with
    | SetRequested cnt ->
        { model with
              RequestedImagesCount = cnt },
        Cmd.none
    | ImageListReceived images ->
        model,
        images
        |> List.map (fun path -> Cmd.ofTaskMsg (downloadImage path))
        |> Cmd.batch
    | Show -> { model with Images = [] }, Cmd.ofTaskMsg (downloadUrs model.RequestedImagesCount)
    | ImageDownloaded image ->
        { model with
              Images = image :: model.Images },
        Cmd.none

let view (model: Model) dispatch =
    View.ContentPage(
        title = "Shibe",
        icon = Image.fromFont (FontImageSource(Glyph = FA.Dog, FontFamily = "FA")),
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
                               <> (model.Images |> List.length))
                      )
                      View.ScrollView(
                          verticalOptions = LayoutOptions.Fill,
                          content =
                              View.StackLayout(
                                  children =
                                      [ for i in model.Images |> List.rev do
                                            View.Image(source = Image.fromBytes i) ]
                              )
                      ) ]
            )
    )
