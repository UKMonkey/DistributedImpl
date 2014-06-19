using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DistributedShared.SystemMonitor
{
    public class RollingAverage
    {
        private readonly int _timeOfAverage;
        private readonly LinkedList<DataPoint> _points = new LinkedList<DataPoint>();
        private readonly Stopwatch _watch = new Stopwatch();

        public double Average { get { return GetAverage(); } }

        private class DataPoint
        {
            public long Time;
            public double Value;
        }


        public RollingAverage(int msec)
        {
            _timeOfAverage = msec;
            _watch.Start();
        }


        public void AddPoint(double value)
        {
            var point = new DataPoint{Time = _watch.ElapsedMilliseconds, Value = value};
            lock (this)
            {
                _points.AddLast(point);
            }
        }


        private double GetAverage()
        {
            var now = _watch.ElapsedMilliseconds;
            var target = now - _timeOfAverage;
            List<DataPoint> points;

            lock (this)
            {
                while (_points.Count > 0 &&
                       _points.First.Value.Time < target)
                    _points.RemoveFirst();

                points = _points.ToList();
            }

            double sum = 0;
            foreach (var value in points.Select(item => item.Value))
                sum += value;

            sum /= _timeOfAverage;
            return sum;
        }
    }
}
