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
} from 'office-ui-fabric-react/lib/Calendar';
import { CodeCellResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import { randomReactKey } from '../utils';

export default function CalendarRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.length > 0 &&
        typeof result.valueRepresentations[0] === 'string' &&
        result.type === 'System.DateTime'
        ? new CalendarRenderer
        : null
}

class CalendarRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        return [{
            displayName: 'Calendar',
            key: randomReactKey(),
            component: CalendarRepresentation,
            componentProps: {
                value: new Date((result.valueRepresentations as any[])[0] as string)
            }
        }]
    }
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

class CalendarRepresentation extends React.Component<{ value: Date }> {
    render() {
        return <Calendar
            strings={DayPickerStrings}
            isMonthPickerVisible={false}
            showGoToToday={false}
            showWeekNumbers={true}
            value={this.props.value}/>
    }
}