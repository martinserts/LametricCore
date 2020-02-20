namespace LaMetric.Kengaroos

open System
open NodaTime
open FSharpx.Text
open FSharpx.Option
open LaMetric.Kengaroos.DataTypes
open Ical.Net
open Ical.Net.CalendarComponents
open Ical.Net.DataTypes
open Ical.Net.Serialization

module IcalGenerator =

    let private findTime (details: string) : LocalTime option =
        maybe {
            let! m = Regex.tryMatch "[^\d*](\d\d)[\.:](\d\d)[^\d*]" details
            let intValues = List.map int m.GroupValues
            return new LocalTime(intValues.[0], intValues.[1])
        }

    let toIcal (days: CalendarDay list) =
        let toCalendarEvent (date: LocalDate) (name: string) = 
            let fullLocalDate = match findTime name with
                                | Some t -> date.At(t)
                                | None -> date.AtMidnight()
            let zone = DateTimeZoneProviders.Tzdb.["Europe/Riga"]
            let e = new CalendarEvent()
            e.Start <- new CalDateTime(fullLocalDate.InZoneLeniently(zone).ToDateTimeUtc())
            e.Duration <- TimeSpan.FromHours(1.0)
            e.Name <- name
            e.Description <- name
            e.Summary <- "Kengaroos"
            e.Group <- "VEVENT"
            e

        let toEvents (day: CalendarDay) : CalendarEvent list =
            day.Events
            |> List.map (toCalendarEvent day.Date)

        let calendar = new Calendar()

        days
        |> List.collect toEvents
        |> List.iter calendar.Events.Add

        calendar


    let serializeCalendar (calendar: Calendar) =
        let serializer = new CalendarSerializer()
        serializer.SerializeToString(calendar)
