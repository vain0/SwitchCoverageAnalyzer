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
        // Tuesday and Wednesday are missing.
        // Unsorted: Friday appears before Thursday.
        switch (wday)
        {
            case DayOfWeek.Sunday:
            case DayOfWeek.Monday:
            case DayOfWeek.Saturday:
                Debug.WriteLine("Sunday or Monday.");
                break;
            case DayOfWeek.Friday:
                Debug.WriteLine("Friday.");
                break;
            case DayOfWeek.Thursday:
                Debug.WriteLine("Thursday.");
                break;
            default:
                break;
        }
    }
}
