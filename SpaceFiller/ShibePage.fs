module SpaceFiller.ShibePage

open System.Net.Http
open FSharp.Control.Tasks
open Fabulous
open Fabulous.XamarinForms
open Newtonsoft.Json
open Xamarin.Forms

type Msg =
    | SetRequested of int
    | ImageDownloaded of string list
    | Show

type Model =
    { RequestedImages: int
      ImageUrls: string list }

let downloadUrs count () =
    task {
        let url =
            $"http://shibe.online/api/shibes?count={count}"

        use httpClient = new HttpClient()
        let! a = httpClient.GetAsync(url)
        let! content = a.Content.ReadAsStringAsync()

        let response =
            JsonConvert.DeserializeObject<string array>(content)

        return response |> List.ofArray |> ImageDownloaded
    }

let init () =
    { RequestedImages = 1; ImageUrls = [] }, Cmd.none

let update msg model =
    match msg with
    | SetRequested cnt -> { model with RequestedImages = cnt }, Cmd.none
    | ImageDownloaded images -> { model with ImageUrls = images }, Cmd.none
    | Show -> model, Cmd.ofTaskMsg (downloadUrs model.RequestedImages)

let view (model: Model) dispatch =
    View.ContentPage(
        content =
            View.StackLayout(
                padding = Thickness 20.0,
                verticalOptions = LayoutOptions.Center,
                children =
                    [ View.Label(
                        text = $"%d{model.RequestedImages}",
                        horizontalOptions = LayoutOptions.Center,
                        width = 200.0,
                        horizontalTextAlignment = TextAlignment.Center
                      )
                      View.Button(
                          text = "Increment",
                          command = (fun () -> dispatch (SetRequested(model.RequestedImages + 1))),
                          horizontalOptions = LayoutOptions.Center
                      )
                      View.Button(
                          text = "Decrement",
                          command = (fun () -> dispatch (SetRequested(model.RequestedImages - 1))),
                          horizontalOptions = LayoutOptions.Center
                      )
                      View.Button(
                          text = "Show",
                          command = (fun () -> dispatch Show),
                          horizontalOptions = LayoutOptions.Center
                      )
                      View.Label(text = (model.ImageUrls |> string), horizontalOptions = LayoutOptions.Center) ]
            )
    )
