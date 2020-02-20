namespace LaMetric.Kengaroos

module DataTypes =

    open NodaTime
    
    type CalendarDay = {
        Date: LocalDate;
        Events: string list;
    }
