using System;

namespace SpeechDemo.Model
{
    internal class SynWatch
    {
        private int _secondCounter;
        private int _milliCounter;

        private int _secondLastCount;
        private int _milliLastCount;

        private bool _running;

        private static int TickInSeconds()
        {
            return (Environment.TickCount / 1000);
        }

        private static int TickInMilliseconds()
        {
            return (Environment.TickCount);
        }


        public SynWatch()
        {
            _running = false;
            _secondLastCount = 0;
            _milliLastCount = 0;

            _secondCounter = TickInSeconds();
            _milliCounter = TickInMilliseconds();
        }

        public static SynWatch StartNew()
        {
            var toReturn = new SynWatch();
            toReturn.Start();

            return toReturn;

        }

        public void Start()
        {
            if (_running == false)
            {
                _secondCounter = TickInSeconds() - _secondLastCount;
                _milliCounter = TickInMilliseconds() - _milliLastCount;
            }
            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        public void Reset()
        {
            _secondLastCount = 0;
            _milliLastCount = 0;

            _running = false;
        }

        public int ElapsedSeconds()
        {
            if (_running)
            {
                _secondLastCount = TickInSeconds() - _secondCounter;
                return _secondLastCount;
            }
            return _secondLastCount;
        }

        public int ElapsedMilliseconds()
        {
            if (_running)
            {
                _milliLastCount = TickInMilliseconds() - _milliCounter;
                return _milliLastCount;
            }
            return _milliLastCount;
        }
    }
}
