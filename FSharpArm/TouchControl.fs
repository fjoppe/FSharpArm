namespace FSharpArm

open WebSharper
open System
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.Core.Resources
open Server


[<Client>]
module TouchControl =
    type TouchButtonValve = UpperValve | LowerValve
    type TouchButtonColor = Resting | SendMessage | Pressed | CommsError

    let TouchButtonWidget valve pin = 
        let buttonColor = Var.Create Resting

        /// sends a control panel command to the server
        let sendControlPanelCommand targetState pin_number command =
            async {
                Var.Set buttonColor SendMessage
                let! result = Server.sendControlPanelCommand pin_number command
                match result with
                | OK    -> Var.Set buttonColor targetState
                | NOK   -> Var.Set buttonColor CommsError
                return ()
            } |> Async.Start

        ///  creates a control panel command for the current valve
        let createEventMessage command =
            match valve with
            | UpperValve  -> Server.DirectionalControllerMessage.UpperValve(command)
            | LowerValve  -> Server.DirectionalControllerMessage.LowerValve(command)


        let openValve (el:Dom.Element) (ev:Dom.Event) =
            sendControlPanelCommand Pressed pin (createEventMessage ControlPanelCommand.OpenValve)
            ev.PreventDefault()

        let closeValve (el:Dom.Element) (ev:Dom.Event) = 
            sendControlPanelCommand Resting pin (createEventMessage ControlPanelCommand.CloseValve)
            ev.PreventDefault()

        let stateToClass =  function
                            | Resting      -> "resting"
                            | SendMessage  -> "waitingResponse"
                            | Pressed      -> "pressed"
                            | CommsError   -> "communicationError"

        let dynamicButtonColor = buttonColor.View |> View.Map (fun s -> sprintf "%s %s" "touchButton" (stateToClass s)) 

        divAttr [
            attr.classDyn dynamicButtonColor
            on.touchstart openValve
            on.touchend   closeValve
            on.mousedown  openValve
            on.mouseup    closeValve            
        ][imgAttr [attr.src "Button.svg"][]]


    let TouchControlWidget pin_number = 
        tableAttr [attr.``class`` "motorCP"] [
            tr[
                td []
                td [imgAttr [attr.src "MicroServoSVG.svg"][]]
                td []
            ]
            tr[
                tdAttr [attr.``class`` "centered"] [TouchButtonWidget UpperValve pin_number]
                tdAttr [attr.``class`` "centered motorNum"] [text (sprintf "%d"  pin_number)]
                tdAttr [attr.``class`` "centered"] [TouchButtonWidget LowerValve pin_number]
            ]
        ]

    