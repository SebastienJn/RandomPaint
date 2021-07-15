using System;
using ColorHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RandomPaint
{
    public class ColorMap : ICloneable
    {
        #region Private

        private readonly int _fullWidth;
        private readonly int _fullHeight;
        private readonly Func<int, int, Texture2D> _onBufferInitialization;

        // Updated in bufferInitialization
        private uint[] _buffer0;
        private uint[] _buffer1;
        private int _width, _height;
        private int _divisor;
        private Texture2D _canvas;

        private int random4Index = 0;
        private static readonly int[] random4 = initializeRandom4();
        private static int[] initializeRandom4()
        {
            var rd = new int[1009];
            for (var index = 0; index < rd.Length; index++)
            {
                rd[index] = Helpers.Random.Next(4);
            }
            return rd;
        }

        #endregion

        public UInt32[] InternalArray => _buffer0;

        public int Width => _width;
        public int Height => _height;
        public int Length => _width * _height;

        public int Divisor => _divisor;

        public Texture2D Canvas => _canvas;

        public ColorMap(int fullWidth, int fullHeight, int divisor, Func<int, int, Texture2D> OnBufferInitialization)
        {
            if (fullWidth % divisor != 0 || fullHeight % divisor != 0)
                throw new ApplicationException("Invalid parameter");

            _fullWidth = fullWidth;
            _fullHeight = fullHeight;
            _onBufferInitialization = OnBufferInitialization;
            _divisor = divisor;
            BufferInitialization();
        }

        public static ColorMap InitializeFromGraphicsDevice(GraphicsDevice graphicsDevice, int initialDivisor)
        {
            var screenSize = graphicsDevice.PresentationParameters.Bounds;

            Texture2D OnBufferInitialization(int width, int height)
            {
                return new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            }

            return new ColorMap(screenSize.Width, screenSize.Height, initialDivisor, OnBufferInitialization);
        }


        /// <summary>
        /// Increase resolution by splitting each pixel to a 2x2 pixel block
        /// </summary>
        /// <returns>false when cannot higher resolution reached</returns>
        public bool ResolutionUpStep()
        {
            var originalBuffer = _buffer0;
            var originalWidth = _width;

            if (_divisor == 1)
                return false; // Cannot go further

            _divisor /= 2;

            BufferInitialization();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _buffer0[x + y * _width] = originalBuffer[(x >> 1) + (y >> 1) * originalWidth];
                }
            }

            return true;
        }

        public bool ResolutionUpStep_To4Identicals()
        {
            var originalBuffer = _buffer0;
            var originalWidth = _width;
            var originalHeight = _height;

            if (_divisor == 1)
                return false; // Cannot go further

            _divisor /= 2;

            BufferInitialization();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _buffer0[x + y * _width] = originalBuffer[(x % originalWidth) + (y % originalHeight) * originalWidth];
                }
            }

            return true;
        }

        public void ResolutionDownStep()
        {
            var originalBuffer = _buffer0;
            var originalWidth = _width;

            _divisor *= 2;

            BufferInitialization();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var originalX = x << 1;
                    var originalY = y << 1;
                    _buffer0[x + y * _width] = originalBuffer[originalX + originalY * originalWidth];
                }
            }
        }

        public void ColorPropagation(IPropagationOptions opt)
        {
            double age = opt.Age;
            double temperature = opt.Temperature;
            double maxtemp = opt.Maxtemp;

            var probabilities = new[]
            {
                maxtemp * 0.9999, // P0 - Keep existing 
                maxtemp * 0.0001, // P1 - Saturation/value variation
                (1 + 9/(age + 1)) * temperature, // P2 - One of neighbor's color
                opt.RandomColorSeed ? temperature / (maxtemp - temperature) : 0 // P3 - Random color
            };

            var actions = new Func<uint[], int, uint>[]
            {
                null, // P0 - Keep existing
                (buffer, i) =>
                {   // P1 - Saturation/value variation
                    var c = new Color(buffer[i]);
                    var hsv = ColorConverter.RgbToHsv(new RGB(c.R, c.G, c.B));
                    if(Helpers.Random.Next(2) == 0)
                        hsv.V=(byte) (hsv.V > 10 ? hsv.V - 10 : 0);
                    else
                        hsv.S=(byte) (hsv.S > 20 ? hsv.S - 20 : 0);
                    var rgb = ColorConverter.HsvToRgb(hsv);
                    return new Color(rgb.R, rgb.G, rgb.B).PackedValue;
                },
                (buffer, i) =>
                {
                    // P2 - One of neighbor's color
                    return buffer[RndNeighbor(i)];
                },
                (buffer, i) =>
                {   // P3 - Random color
                    var rgb = ColorConverter.HsvToRgb(new HSV(Helpers.Random.Next(360), 100, 100));
                    return  new Color(rgb.R, rgb.G, rgb.B).PackedValue;
                }
            };

            var switcher = new RandomSwitcher(probabilities);

            Propagation(actions, switcher);
        }

        public ColorMap GetHalfSizeSample(int x, int y)
        {
            var colorMap = new ColorMap(this._fullWidth, this._fullHeight, _divisor * 2, _onBufferInitialization);

            int j = 0;
            for (int iy = 0; iy < colorMap._height; iy++)
                for (int ix = 0; ix < colorMap._width; ix++)
                {
                    colorMap._buffer0[j++] = _buffer0[x + ix + (y + iy) * _width];
                }

            return colorMap;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle bounds)
        {
            Canvas.SetData(InternalArray, 0, Length);
            spriteBatch.Draw(Canvas, new Rectangle(0, 0, bounds.Width, bounds.Height), Color.White);
        }


        #region Internal Utilities

        private void BufferInitialization()
        {
            _width = _fullWidth / _divisor;
            _height = _fullHeight / _divisor;
            _buffer0 = new UInt32[_width * _height];
            _buffer1 = new UInt32[_width * _height];
            _canvas = _onBufferInitialization(_width, _height);
        }

        private int RndNeighbor(int origine)
        {
            var rnd = random4[random4Index];
            if (--random4Index < 0)
                random4Index = random4.Length - 1;

            int Out = origine;
            var bufferLength = _buffer0.Length;
            switch (rnd)
            {
                case 0:
                    Out -= _width; goto decreased;
                case 1:
                    Out -= 1; goto decreased;

                case 2:
                    Out += 1; goto increased;
                case 3:
                    Out += _width; goto increased;

                default:
                    throw new NotSupportedException();
            }

            decreased:
            if (Out < 0)
                return bufferLength + Out;
            else
                return Out;

            increased:
            if (Out >= bufferLength)
                return Out - bufferLength;
            else
                return Out;
        }

        private void Propagation(Func<uint[], int, uint>[] actions, RandomSwitcher switcher)
        {
            for (int i = _buffer0.Length - 1; i >= 0; i--)
            {
                var selection = switcher.RandomSwitch();
                uint newValue = 0;

                switch (selection)
                {
                    case 0:
                        {
                            newValue = _buffer0[i];
                        }
                        break;
                    case 1:
                        {   // P1 - Saturation/value variation
                            var c = new Color(_buffer0[i]);
                            var hsv = ColorConverter.RgbToHsv(new RGB(c.R, c.G, c.B));
                            if (Helpers.Random.Next(2) == 0)
                                hsv.V = (byte)(hsv.V > 10 ? hsv.V - 10 : 0);
                            else
                                hsv.S = (byte)(hsv.S > 20 ? hsv.S - 20 : 0);
                            var rgb = ColorConverter.HsvToRgb(hsv);
                            newValue = new Color(rgb.R, rgb.G, rgb.B).PackedValue;
                        }
                        break;
                    case 2:
                        {
                            // P2 - One of neighbor's color
                            newValue = _buffer0[RndNeighbor(i)];
                        }
                        break;
                    case 3:
                        {   // P3 - Random color
                            var rgb = ColorConverter.HsvToRgb(new HSV(Helpers.Random.Next(360), 100, 100));
                            newValue = new Color(rgb.R, rgb.G, rgb.B).PackedValue;
                        }
                        break;
                }

                _buffer1[i] = newValue;
            }

            // Switch buffers
            var tmp = _buffer0;
            _buffer0 = _buffer1;
            _buffer1 = tmp;
        }

        #endregion

        #region Implementation of ICloneable

        public object Clone()
        {
            var clone = new ColorMap(this._fullWidth, this._fullHeight, this.Divisor, _onBufferInitialization);

            _buffer0.CopyTo(clone._buffer0, 0);

            return clone;
        }

        #endregion
    }
}