using System;
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
            case DayOfWeek.Thursday:
                Debug.WriteLine("Thursday.");
                break;
            case DayOfWeek.Friday:
                Debug.WriteLine("Friday.");
                break;
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Saturday:
            default:
                break;
        }
    }
}
