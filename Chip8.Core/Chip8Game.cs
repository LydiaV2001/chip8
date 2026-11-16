using System;
using Chip8.Core.Localization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using static System.Net.Mime.MediaTypeNames;

namespace Chip8.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    public class Chip8Game : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphicsDeviceManager;
        private Canvas _canvas;
        private SpriteBatch _spriteBatch;

        private Chip8 _chip8;
        
        // input bindings
        Keys[] keyBindings = [Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4,
        Keys.Q, Keys.W, Keys.E, Keys.R,
        Keys.A, Keys.S, Keys.D, Keys.F,
        Keys.Z, Keys.X, Keys.C, Keys.F];

        public Chip8Game()
        {
            IsFixedTimeStep = false;
            
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            graphicsDeviceManager.PreferredBackBufferWidth = 64;
            graphicsDeviceManager.PreferredBackBufferHeight = 32;
            graphicsDeviceManager.ApplyChanges();

            // Share GraphicsDeviceManager as a service.
            Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);
            
            // setup canvas
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _canvas = new Canvas(graphicsDeviceManager.GraphicsDevice, 64, 32);
            _canvas.SetDestinationRectangle();
            
            Content.RootDirectory = "Content";

            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations =
                DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        protected override void Initialize()
        {
            // rendering at a scale of 8
            SetResolution(512, 256);
            
            _chip8 = new Chip8("Content/Chip8Roms/spaceinvaders.ch8");
            
            base.Initialize();

        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            
            GetKeys();
            _chip8.EmulateCycle();
            
            if (_chip8.delayTimer > 0)
            {
                _chip8.delayTimer -= 1;
            }

            if (_chip8.soundTimer > 0)
            {
                if (_chip8.soundTimer == 1)
                {
                    Debug.WriteLine("Beep Sound Goes Here");
                    --_chip8.soundTimer;
                }
            }
            
            base.Update(gameTime);
        }

        private void GetKeys()
        {
            KeyboardState state = Keyboard.GetState();

            for (int i = 0; i < keyBindings.Length; i++)
            {
                if (state.IsKeyDown(keyBindings[i]))
                {
                    _chip8.key[i] = true;
                    //Debug.WriteLine("Key: " + keyBindings[i] + "is down.");
                }
                else
                {
                    _chip8.key[i] = false;
                }
            }
        }

        // renders the VM's display values into the spritebatch
        private void RenderSpritebatch()
        {
            _spriteBatch.Begin();
            
            // for loop
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    if (_chip8.display[i, j] == true)
                    {
                        // render white pixel at location i, j
                        _spriteBatch.DrawPoint(new Vector2(i, j), Color.White);
                        
                    }
                    else
                    {
                        // render black pixel at location i, j
                        _spriteBatch.DrawPoint(new Vector2(i, j), Color.Black);
                    }
                }
            }
            
            _spriteBatch.End();
        }
        
        private void SetResolution(int width, int height)
        {
            graphicsDeviceManager.PreferredBackBufferWidth = width;
            graphicsDeviceManager.PreferredBackBufferHeight = height;
            Window.IsBorderless = false;
            graphicsDeviceManager.ApplyChanges();
            _canvas.SetDestinationRectangle();
        }
        
        protected override void Draw(GameTime gameTime)
        {
            
            _canvas.Activate();
            GraphicsDevice.Clear(Color.CornflowerBlue);
            RenderSpritebatch();
            _canvas.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}