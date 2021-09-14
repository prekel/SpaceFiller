module SpaceFiller.Android.Services

open SpaceFiller.Services
open SpaceFiller

type AndroidFreeStorage() =
    interface IFreeStorage with
        member _.GetFreeStorage() =
            Android.OS.Environment.DataDirectory.UsableSpace

[<assembly: Xamarin.Forms.Dependency(typeof<AndroidFreeStorage>)>]
do ()
