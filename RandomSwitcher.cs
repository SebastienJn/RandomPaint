using System;

namespace RandomPaint
{
    class RandomSwitcher
    {
        #region Private

        private readonly double[] _proba;

        private readonly double[] _scale;
        
        private readonly double _integral;

        private int randomNbIndex;

        private static double[] randomNumbers = initializeRandomNumbers();

        private static double[] initializeRandomNumbers()
        {
            randomNumbers = new double[1046527];
            for (var index = 0; index < randomNumbers.Length; index++)
            {
                randomNumbers[index] = Helpers.Random.NextDouble();
            }
            return randomNumbers;
        }

        #endregion

        public RandomSwitcher(double[] proba)
        {
            _proba = proba;
            _integral = 0.0;
            int _probaLength = _proba.Length;

            _scale = new double[_probaLength];

            for (var i = 0; i < _probaLength; i++)
            {
                _integral += _proba[i];
                _scale[i] = _integral;
            }

            randomNbIndex = Helpers.Random.Next(randomNumbers.Length);
        }

        public int RandomSwitch()
        {
            double r = randomNumbers[randomNbIndex] * _integral;
            if (--randomNbIndex < 0)
                randomNbIndex = randomNumbers.Length - 1;

            for (var i = 0; i < _proba.Length; i++)
            {
                if (r <= _scale[i])
                {
                    return i;
                }
            }

            throw new ApplicationException("Invalid execution path");
        }
    }
}