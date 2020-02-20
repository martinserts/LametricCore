namespace LaMetric.Common

module DataTypes =

    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp

    open LaMetric.Common.Icons
    open LaMetric.Common.Sound

    [<LowerCase>]
    type NotificationPriority = Info | Warning | Critical

    [<LowerCase>]
    type NotificationIconType = None | Info | Alert
  
    type NotificationSimpleFrame = {
        index: int option;
        icon: NotificationIcon option;
        text: string
    }

    type NotificationGoalData = {
        start: int;
        current: int;
        ``end``: int;
        unit: string option;
    }

    type NotificationGoalFrame = {
        icon: NotificationIcon option;
        goalData: NotificationGoalData;
    }

    type NotificationChartFrame = {
        chartData: int list
    }

    type NotificationFrame =
        | NotificationSimpleFrame of NotificationSimpleFrame
        | NotificationGoalFrame of NotificationGoalFrame
        | NotificationChartFrame of NotificationChartFrame
    
    
    type NotificationModel = {
        frames: NotificationFrame list;
        sound: NotificationSound option;
        cycles: int option
    }

    type Notification = {
        priority: NotificationPriority option;
        icon_type: NotificationIconType option;
        lifeTime: int option;
        model: NotificationModel;
    }