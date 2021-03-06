﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class Testee
{
    public void Method(DayOfWeek wday)
    {
        // Tuesday, Wednesday and Saturday are missing.
        switch (wday)
        {
            case DayOfWeek.Sunday:
            case DayOfWeek.Monday:
                Debug.WriteLine("Sunday or Monday.");
                break;
            case DayOfWeek.Tuesday:
                throw new NotImplementedException();
            case DayOfWeek.Wednesday:
                throw new NotImplementedException();
            case DayOfWeek.Thursday:
                Debug.WriteLine("Thursday.");
                break;
            case DayOfWeek.Friday:
                Debug.WriteLine("Friday.");
                break;
            case DayOfWeek.Saturday:
                throw new NotImplementedException();
            default:
                break;
        }
    }
}
