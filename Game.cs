using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net.Sockets;
using ColorHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RandomPaint
{
    public class Game : Microsoft.Xna.Framework.Game
    {
        // Parameters
        private const int InitialDivisor = 32;

        // Inner objects 
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Generator _generator;

        private readonly SelectionMode _selectionMode = new SelectionMode();

        // States

        private ColorMap _currentColorMap = null;

        private ColorMap _lastSelectedColorMap = null;

        private enum State
        {
            FirstCoolDown,
            SelectNextSubMap,
            Reprocess,
            FinalCoolDown,
            Stopped
        }


        // Global state
        private State currentState = State.FirstCoolDown;

        private double age = 0.0;

        // Selection step

        public Game()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        // 0 - Propagation in coarse resolutions
        // 1 - Finest resolution reached, final propagation
        // 2 - Finished 

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            _graphics.IsFullScreen = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.ApplyChanges();

            _generator = new Generator(ColorMap.InitializeFromGraphicsDevice(GraphicsDevice, InitialDivisor));
            _currentColorMap = _generator.ColorMap;

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000 / 25f);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            // Poll for current keyboard state
            KeyboardState keyboardState = Keyboard.GetState();

            // If they hit esc, exit
            var propagationOptions = (IPropagationOptions) _generator;
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                if(_lastSelectedColorMap == null)
                {
                    Exit();
                }
                else
                {
                    _currentColorMap = _lastSelectedColorMap;

                    _generator.ColorMap = _currentColorMap;
                    _generator.CurrentState = Generator.State.CoolDown;
                    _generator.SetTemperature(propagationOptions.Maxtemp / 30);

                    currentState = State.FinalCoolDown;
                    age = 2;
                }
            }

            bool generatorIsRunning = false;

            switch (currentState)
            {
                case State.FirstCoolDown:
                    generatorIsRunning = true;
                    break;
                case State.SelectNextSubMap:
                    {
                        bool selectCurrent = keyboardState.IsKeyDown(Keys.Space);
                        if (!_selectionMode.Update_SelectionMode(selectCurrent, ref _currentColorMap))
                        { // On selection mode exit

                            _lastSelectedColorMap = (ColorMap) _currentColorMap.Clone();
                            
                            _generator.ColorMap = _currentColorMap;
                            _generator.CurrentState = Generator.State.WarmUp;
                            currentState = State.Reprocess;

                            _currentColorMap.ResolutionUpStep_To4Identicals();

                            age += 1;
                        }
                    }
                    break;
                case State.Reprocess:
                    generatorIsRunning = true;
                    break;
                case State.FinalCoolDown:
                    generatorIsRunning = true;
                    _generator.DisableRandomColorSeed();
                    break;
                case State.Stopped:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (generatorIsRunning)
            {
                Generator.State gstate = _generator.Update(age);

                switch (gstate)
                {
                    case Generator.State.StoppedDown:
                        if (currentState == State.FinalCoolDown)
                        { // Save and exit
                            currentState = State.Stopped;
                            generatorIsRunning = false;
                            var pngFile = Path.GetTempPath() + DateTime.Now.ToString("yy-MM-dd_HHmmss") + ".png";
                            using (Stream stream = new FileStream(pngFile,
                                FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                var canvas = _generator.ColorMap.Canvas;
                                canvas.SaveAsJpeg(stream, canvas.Width, canvas.Height);
                            }

                            Helpers.ShowFileInExplorer(pngFile);
                            Exit();
                        }
                        else
                        {
                            _generator.ColorMap = _generator.ColorMap.GetHalfSizeSample(0, 0);
                            this.currentState = State.SelectNextSubMap;
                        }
                        break;
                    case Generator.State.WarmUp:
                        double threashold = propagationOptions.Maxtemp * 0.5 * (0.2 + 0.8 / (age + 1));
                        Debug.WriteLine("threashold = " + threashold);
                        if (propagationOptions.Temperature > threashold)
                        {
                            _generator.CurrentState = Generator.State.CoolDown;
                        }
                        break;
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Red);

            GraphicsDevice.Textures[0] = null;

            var bounds = GraphicsDevice.PresentationParameters.Bounds;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_currentColorMap != null)
            {
                _currentColorMap.Draw(_spriteBatch, bounds);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
