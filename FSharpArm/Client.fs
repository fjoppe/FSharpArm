namespace FSharpArm

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

open TouchControl

[<JavaScript>]
module Client =

    let Main () =
        div [
        
            tableAttr [attr.border "1"; attr.width "100%"; attr.height "100%"] [
                tr[
                    tdAttr[attr.``class`` "controller"] [
                        TouchControlWidget 6
                    ]
                    tdAttr[attr.``class`` "controller"] [
                        TouchControlWidget 5
                    ]
                ]
                tr[
                    tdAttr[attr.``class`` "controller"] [
                        TouchControlWidget 4
                    ]
                    tdAttr[attr.``class`` "controller"] [
                        TouchControlWidget 2
                    ]
                ]
            ]
        ]
