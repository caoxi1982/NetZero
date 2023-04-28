using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetZero
{
    internal class TimeDuration
    {
        private TimeOnly Start { get; }
        private TimeOnly End { get; }

        public TimeDuration(TimeOnly start,TimeOnly end) 
        {
            Start = start;
            End = end;
        }
        public bool IsInTimeRange(TimeOnly time)
        {
            if (Start < End)
            {
                return time >= Start && time <= End;
            }
            else
            {
                return false;
            }
        }
    }
}
