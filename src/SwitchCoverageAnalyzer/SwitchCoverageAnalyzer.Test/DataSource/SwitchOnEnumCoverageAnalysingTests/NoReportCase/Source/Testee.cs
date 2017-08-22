using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Testee
{
    enum Priority
    {
        Low,
        Middle,
        High,
    }

    public void SwitchOnEnumWithoutDefault(Priority priority)
    {
        switch (priority)
        {
            case Priority.Low:
                Debug.WriteLine("Low.");
                break;
            case Priority.Middle:
                Debug.WriteLine("Middle.");
                break;
            case Priority.High:
                Debug.WriteLine("High.");
                break;
        }
    }

    public string SwitchOnEnumWithDefault(Priority priority)
    {
        switch (priority)
        {
            case Priority.Low:
                return "★☆☆";
            case Priority.Middle:
                return "★★☆";
            case Priority.High:
                return "★★★";
            default:
                throw new Exception();
        }
    }

    public Priority SwitchOnString(string keyword)
    {
        switch (keyword)
        {
            case "TODO":
                return Priority.Middle;
            case "FIXME":
                return Priority.High;
            default:
                return Priority.Low;
        }
    }

    public Priority SwitchOnInt(int value)
    {
        switch (value)
        {
            case 0:
                return Priority.High;
            case 1:
                return Priority.Middle;
            case 2:
                return Priority.Low;
            default:
                throw new Exception();
        }
    }
}
