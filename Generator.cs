using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RandomPaint
{
    public class Generator : IPropagationOptions
    {
        #region Private

        // IPropagationOptions
        private const double _maxtemp = 1000;
        private double _temperature = _maxtemp;
        private bool _randomColorSeed = true;
        private int _age = 0;

        private State _currentState = State.CoolDown;

        private ColorMap _colorMap;

        #endregion

        public Generator(ColorMap colorMap)
        {
            _colorMap = colorMap;
        }

        public enum State
        {
            CoolDown,
            LastCoolDown,
            StoppedDown,
            WarmUp
            //StoppedUp
        }

        public ColorMap ColorMap
        {
            get => _colorMap;
            set => _colorMap = value;
        }

        public State Update(double age)
        {
            const double coolDownFactor = 0.985;

            switch (_currentState)
            {
                case State.CoolDown:
                case State.LastCoolDown:
                    {
                        _colorMap.ColorPropagation(this);
                        _temperature *= coolDownFactor;

                        var lowerBound = _colorMap.Divisor * Maxtemp / 100;
                        if (_temperature < lowerBound)
                        #region Increase resolution when temperature decrease
                        {
                            switch (_currentState)
                            {
                                case State.CoolDown:
                                    if (!_colorMap.ResolutionUpStep())
                                    {
                                        _currentState = State.LastCoolDown; // Final resolution reached
                                    }
                                    break;
                                case State.LastCoolDown:
                                    _currentState = State.StoppedDown; // Final temperature reached -> done
                                    break;
                            }
                        }
                        #endregion
                    }
                    break;
                case State.WarmUp:
                    {
                        _colorMap.ColorPropagation(this);

                        _temperature /= coolDownFactor;

                        var upperBound = _colorMap.Divisor * 2 * _maxtemp / 100;

                        if (_temperature > upperBound)
                        #region Decrease resolution when temperature increase
                        {
                            _colorMap.ResolutionDownStep();
                        }
                        #endregion
                    }
                    break;
            }

            return _currentState;
        }

        public void SetTemperature(double t) => this._temperature = t;

        public void DisableRandomColorSeed()
        {
            this._randomColorSeed = false;
        }

        public State CurrentState
        {
            get => this._currentState;
            set => this._currentState = value;
        }

        #region IPropagationOptions members

        public double Temperature => this._temperature;

        public double Maxtemp => _maxtemp;

        public bool RandomColorSeed => _randomColorSeed;

        public int Age => _age;

        #endregion
    }

    public interface IPropagationOptions
    {
        public double Temperature  { get; }
        public double Maxtemp { get; }

        public bool RandomColorSeed { get; }
        int Age { get; }
    }
}