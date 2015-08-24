namespace FSharpArm

open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets
open System.IO

module main =

    let MySite =
        Warp.CreateSPA (fun ctx ->            
            let styleSheet = File.ReadAllText("../ServoController.css")
            [
                Tags.Style [Text styleSheet]    //  Didn't find a way to do this in the HTML header
                H1 [Text "MeArm Control Panel"]
                Div [ClientSide <@ Client.Main() @>]
            ])

    [<EntryPoint>]
    let main argv =
        do Warp.RunAndWaitForInput(MySite, rootDir = argv.[0], url = argv.[1]) |> ignore
        0
