//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import {
    Calendar,
    DayOfWeek
} from 'office-ui-fabric-react/lib/Calendar'
import { CodeCellResult } from '../evaluation'
import { createComponentRepresentation } from '../rendering'

export default function createCalenderRepresentation(result: CodeCellResult) {
    if (result.resultType === 'System.DateTime' && typeof result.resultRepresentations[0] === 'string')
        return createComponentRepresentation(
            'Calendar',
            CalendarRenderer,
            {
                value: new Date(result.resultRepresentations[0])
            })
    return null
}

const DayPickerStrings = {
    months: [
        'January',
        'February',
        'March',
        'April',
        'May',
        'June',
        'July',
        'August',
        'September',
        'October',
        'November',
        'December'
    ],

    shortMonths: [
        'Jan',
        'Feb',
        'Mar',
        'Apr',
        'May',
        'Jun',
        'Jul',
        'Aug',
        'Sep',
        'Oct',
        'Nov',
        'Dec'
    ],

    days: [
        'Sunday',
        'Monday',
        'Tuesday',
        'Wednesday',
        'Thursday',
        'Friday',
        'Saturday'
    ],

    shortDays: [
        'S',
        'M',
        'T',
        'W',
        'T',
        'F',
        'S'
    ],

    goToToday: 'Go to today'
}

class CalendarRenderer extends React.Component<{ value: Date }> {
    render() {
        return <Calendar
            strings={DayPickerStrings}
            isMonthPickerVisible={false}
            showGoToToday={false}
            showWeekNumbers={true}
            value={this.props.value}/>
    }
}