namespace FSharpArm

open Akka.FSharp
open System
open System.IO
open WebSharper

[<AutoOpen>]
module Server = 

    [<Literal>]
    let defaultSpeed = 10

    [<Literal>]
    let pressurePulsesInterval_ms = 20.0

    [<Literal>]
    let amountOfPins = 6

    /// represents the current state of a valve
    type ValveState   = Open      | Closed

    /// a command to change the valve state
    type ControlPanelCommand = OpenValve | CloseValve 


    /// you can send these messages to the directional controller actor
    type DirectionalControllerMessage = 
        |   PressurePulse
        |   UpperValve of ControlPanelCommand
        |   LowerValve of ControlPanelCommand


    /// holds the directional controller's configuration and vale's state
    type DirectionalControllerState = {
        Speed      : int;
        UpperValve : ValveState;
        LowerValve : ValveState;
    }
    with
        static member create() = {Speed = defaultSpeed; UpperValve = Closed; LowerValve = Closed }

        member private this.commandToState c =
            match c with
            | OpenValve    -> Open
            | CloseValve   -> Closed
         
        member this.ConfigureSpeed v   = {this with Speed = v}
        member this.ChangeUpperValve e = {this with UpperValve = (this.commandToState e)} 
        member this.ChangeLowerValve e = {this with LowerValve = (this.commandToState e)} 


    /// collection of all servo controller actors, mapped on their pin-numbers
    let directional_controllers = 
        //  lesson: if you create 'actorSystem' (below) with "use" instead of "let"
        //  everything is disposed when you exit this function, and you cannot send messages
        let actorSystem = System.create "me-arm" (Configuration.load())

        //  lesson: don't open the device -in- the actor function, it is not a static value
        //  if you do open it in the actor, you will get a sharing violation, the second time it is called
        let device = new StreamWriter(@"/dev/servoblaster", false)  // false = no append, a /dev does not have a seek function
        let console = new StreamWriter(Console.OpenStandardOutput())   // to create some logging

        //  how to create and run an actor with one line of code.. awesome!
        let device_actor = 
            spawn actorSystem "device_writer" (actorOf
                (fun msg -> 
                    fprintfn device "%s" msg
                    fprintfn console "%s" msg                    
                    device.Flush()
                    console.Flush()
                ))

        ///  all directional controllers, one per servo motor
        let directional_controller_actor_list =
            //  lesson: use helper functions for debugging
            //  we create helper functions here to be able to set breakpoints in VS2105.
            //  if we embed the functions in the "spawn" below, then a breakpoint will cover actor creation only
            //  and then you cannot debug the function itself.


            /// send a message to the device actor, depending on the controller_state
            let sendMessageToDeviceActor controller_state pin_number = 
                let pulse_interval = controller_state.Speed // intermediate values like this, makes refactoring easier
                let distance = 
                    match (controller_state.UpperValve, controller_state.LowerValve) with
                    | (Closed, Open) -> -pulse_interval
                    | (Open, Closed) ->  pulse_interval
                    | _ -> 0
                if distance <> 0 then  device_actor <! (sprintf "%d=%+dus" pin_number distance) // number format is crucial


            /// process a message to the directional-controller actor
            let process_controller_message motor_num (mailbox:Actor<DirectionalControllerMessage>) = 
                let rec loop (motorState:DirectionalControllerState) = actor {
                    let! msg = mailbox.Receive()

                    match msg with
                    |   UpperValve e            -> return! loop (motorState.ChangeUpperValve e)
                    |   LowerValve e            -> return! loop (motorState.ChangeLowerValve e)
                    |   PressurePulse           -> sendMessageToDeviceActor motorState motor_num
                                                   return! loop motorState
                }
                loop (DirectionalControllerState.create())


            // main function body
            [0 .. amountOfPins] 
            |> List.map(fun pin_number ->
                let actor_name = sprintf "motor-%02d" pin_number
                let directional_controller_actor =  spawn actorSystem actor_name (process_controller_message pin_number)
                (pin_number, directional_controller_actor)    // we do this for the Map.ofList below
                )
            |> Map.ofList

        ///  create pressure pulsars for each directional-controller actor
        let pulsar = 
            directional_controller_actor_list
            |> Map.toList
            |> List.map(
                fun (pin_number, directional_controller_actor) -> 
                    actorSystem
                        .Scheduler
                        .ScheduleTellRepeatedly(
                            TimeSpan.FromSeconds(0.0),
                            TimeSpan.FromMilliseconds(pressurePulsesInterval_ms),
                            directional_controller_actor, PressurePulse))
        
        //  return the list with controllers
        directional_controller_actor_list    


    /// all the possible responses from a server request
    type CommandResponse = OK | NOK


    [<Remote>]
    /// receive messages from the control panel
    let sendControlPanelCommand (pin:int) (command:DirectionalControllerMessage) =
        async {
            try            
                if (directional_controllers.ContainsKey pin) then
                    directional_controllers.[pin] <! command
                    return OK
                else
                    return NOK
            with
            | _ -> return NOK
        }

