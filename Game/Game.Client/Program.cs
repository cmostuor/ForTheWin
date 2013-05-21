﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Window;
using SFML.Graphics;
using FTW.Engine.Client;

namespace Game.Client
{
    static class Program
    {
        static void Main()
        {
            // Create the main window
            RenderWindow window = new RenderWindow(new VideoMode(800, 600, 32), "FTW Example", Styles.Default, new ContextSettings(32, 0));
            window.SetVerticalSyncEnabled(true);

            // Setup event handlers
            window.Closed += new EventHandler(OnClosed);
            window.KeyPressed += new EventHandler<KeyEventArgs>(OnKeyPressed);
            window.Resized += new EventHandler<SizeEventArgs>(OnResized);

            CreateMainMenu(window);
            CreateOptionsMenu(window);
            SetCurrentMenu(mainMenu);

            while (window.IsOpen())
            {
                window.DispatchEvents();

                window.Clear();

                window.Draw(currentMenu);

                window.Display();
            }
        }

        private static void SetCurrentMenu(Menu menu)
        {
            if (currentMenu != null)
                currentMenu.DisableInput();

            currentMenu = menu;

            if ( currentMenu != null )
                currentMenu.EnableInput();
        }

        static Menu currentMenu, mainMenu, optionsMenu;

        private static void CreateMainMenu(RenderWindow window)
        {
            mainMenu = new Menu(window)
            {
                ItemFont = new Font("Resources/arial.ttf"),
                ItemYPos = 96,
                ValueXOffset = 256,
                ItemColor = Color.Green,
                HoverItemColor = Color.Yellow,
                HoverItemStyle = Text.Styles.Underlined
            };

            mainMenu.EscapePressed = () => { CloseGameWindow(window); Console.WriteLine("escape pressed"); };

            mainMenu.AddItem(new Menu.LinkItem("Host game", HostGame));
            mainMenu.AddItem(new Menu.LinkItem("Join game", JoinGame));
            mainMenu.AddItem(new Menu.LinkItem("Options", () => SetCurrentMenu(optionsMenu)));
            mainMenu.AddItem(new Menu.LinkItem("Quit", () => { CloseGameWindow(window); Console.WriteLine("close clicked"); }));
        }

        private static void CreateOptionsMenu(RenderWindow window)
        {
            optionsMenu = new Menu(window);
            optionsMenu.CopyStyling(mainMenu);

            optionsMenu.AddItem(new Menu.ListItem("Choice:", new string[] { "Option 1", "Option 2", "Option 3" }, (string value) => Console.WriteLine(value + " selected")));
            optionsMenu.AddItem(new Menu.TextEntryItem("Name:", "Player", 12, (string value) => Console.WriteLine("Name changed: " + value)));
            optionsMenu.AddItem(new Menu.LinkItem("Back", () => SetCurrentMenu(mainMenu)));

            optionsMenu.EscapePressed = () => { SetCurrentMenu(mainMenu); };
        }

        static ServerConnection connection = null;

        private static void HostGame()
        {
            if (connection != null)
                return;

            Console.WriteLine("host clicked");

            connection = new ListenServerConnection();
            connection.Connect();
        }

        private static void JoinGame()
        {
            if (connection != null)
                return;

            Console.WriteLine("join clicked");

            connection = new RemoteClientConnection("127.0.0.1", 24680);
            connection.Connect();
        }

        /// <summary>
        /// Function called when the window is closed
        /// </summary>
        static void OnClosed(object sender, EventArgs e)
        {
            CloseGameWindow(sender as RenderWindow);
        }

        static void CloseGameWindow(RenderWindow window)
        {   
            window.Close();

            if (connection != null)
            {
                connection.Disconnect();
                connection = null;
            }
        }

        /// <summary>
        /// Function called when a key is pressed
        /// </summary>
        static void OnKeyPressed(object sender, KeyEventArgs e)
        {
            /*Window window = (Window)sender;
            if (e.Code == Keyboard.Key.Escape)
                window.Close();*/
        }

        /// <summary>
        /// Function called when the window is resized
        /// </summary>
        static void OnResized(object sender, SizeEventArgs e)
        {
            //Gl.glViewport(0, 0, (int)e.Width, (int)e.Height);
        }
    }
}
