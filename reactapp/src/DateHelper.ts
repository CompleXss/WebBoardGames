export function getDatesDiff_milliseconds(date1: Date, date2: Date): number {
    // wtf
    date1 = new Date(date1);
    date2 = new Date(date2);

    return date2.getTime() - date1.getTime();
}

export function getDatesDiff_string(date1: Date, date2: Date): string {
    var mills = getDatesDiff_milliseconds(date1, date2);

    let seconds = Math.round(mills / 1000);
    let minutes, hours, days;

    [minutes, seconds] = splitTimeValue(seconds, 60);
    [hours, minutes] = splitTimeValue(minutes, 60);
    [days, hours] = splitTimeValue(hours, 24);

    let secondsPart = seconds > 0 ? seconds + ' сек, ' : ''
    let minutesPart = minutes > 0 ? minutes + ' мин, ' : ''
    let hoursPart = hours > 0 ? getHoursString(hours) + ', ' : ''
    let daysPart = days > 0 ? getDaysString(days) + ', ' : ''

    let res = daysPart + hoursPart + minutesPart + secondsPart
    return res === '' ? '0 сек' : res.slice(0, -2)
}

function splitTimeValue(valueToSplit: number, maxValue: number) {
    let biggerValue = 0;

    if (valueToSplit > maxValue) {
        biggerValue = Math.floor(valueToSplit / maxValue);
        valueToSplit -= biggerValue * maxValue;
    }

    return [biggerValue, valueToSplit]
}

function getHoursString(hours: number): string {
    if (hours > 10 && hours < 20) {
        return hours + ' часов'
    }

    switch (hours % 10) {
        case 1:
            return hours + ' час'

        case 2:
        case 3:
        case 4:
            return hours + ' часа'

        default:
            return hours + ' часов'
    }
}

function getDaysString(days: number): string {
    if (days > 10 && days < 20) {
        return days + ' дней'
    }

    switch (days % 10) {
        case 1:
            return days + ' день'

        case 2:
        case 3:
        case 4:
            return days + ' дня'

        default:
            return days + ' дней'
    }
}