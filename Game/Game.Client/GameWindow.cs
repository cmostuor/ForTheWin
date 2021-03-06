﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;
using FTW.Engine.Shared;

namespace Game.Client
{
    public class GameWindow : RenderWindow
    {
        static void Main()
        {
            new GameWindow();
        }

        public Menu MainMenu { get; private set; }
        public Menu InGameMenu { get; private set; }
        public Menu OptionsMenu { get; private set; }
        public Menu CurrentMenu
        {
            get { return currentMenu; }
            set
            {
                currentMenu = value;
                CurrentInput = currentMenu ?? (InputListener)gameClient;
            }
        }

        Menu currentMenu;
        public InputListener CurrentInput;
        GameClient gameClient;
        Config config;

        public GameWindow() :
            base(new VideoMode(800, 600, 32), "FTW Example", Styles.Default, new ContextSettings(32, 0))
        {
            SetVerticalSyncEnabled(true);
            
            // Setup event handlers
            KeyPressed += OnKeyPressed;
            KeyReleased += OnKeyReleased;
            TextEntered += OnTextEntered;
            MouseButtonPressed += OnMousePressed;
            MouseButtonReleased += OnMouseReleased;
            MouseMoved += OnMouseMoved;
            Closed += OnClosed;
            Resized += GameWindow_Resized;

            LoadConfig();

            CreateMenus();
            CurrentMenu = MainMenu;

            gameClient = new GameClient(this, config);
            gameClient.Disconnected += (object o, EventArgs e) => CurrentMenu = MainMenu;

            while (IsOpen())
            {
                DispatchEvents();

                Clear();

                if ( CurrentMenu != null )
                    Draw(CurrentMenu);

                if (gameClient != null && gameClient.Connected)
                    Draw(gameClient);

                Display();
            }
        }

        void GameWindow_Resized(object sender, SizeEventArgs e)
        {// instead of scaling the contents, resize to fit the contents. We probably don't want that, do we?
            gameClient.MainView = new View(new FloatRect(0, 0, e.Width, e.Height));
            SetView(gameClient.MainView);
        }

        private void CreateMenus()
        {
            MainMenu = new Menu(this)
            {
                ItemFont = new Font("Resources/arial.ttf"),
                ItemYPos = 96,
                ValueXOffset = 256,
                ItemColor = Color.Green,
                HoverItemColor = Color.Yellow,
                HoverItemStyle = Text.Styles.Underlined
            };

            MainMenu.EscapePressed = () => { CloseGameWindow(); };

            MainMenu.AddItem(new Menu.LinkItem("Host game", () => { gameClient.ConnectLocal(); CurrentMenu = null; }));
            MainMenu.AddItem(new Menu.LinkItem("Join game", () =>
            {
                string ip = config.FindValueOrDefault(GameClient.config_ServerIP, GameClient.defaultServerIP);

                Config port = config.Find(GameClient.config_ServerPort);
                ushort iPort;
                if (port == null || !ushort.TryParse(port.Value, out iPort))
                    iPort = GameClient.defaultServerPort;

                gameClient.ConnectRemote(ip, iPort); CurrentMenu = null;
            }));
            MainMenu.AddItem(new Menu.LinkItem("Options", () => CurrentMenu = OptionsMenu));
            MainMenu.AddItem(new Menu.LinkItem("Quit", () => { CloseGameWindow(); }));



            InGameMenu = new Menu(this);
            InGameMenu.CopyStyling(MainMenu);

            InGameMenu.AddItem(new Menu.LinkItem("Resume", () => { CurrentMenu = null; }));
            InGameMenu.AddItem(new Menu.LinkItem("Disconnect", () => { EndGame(); CurrentMenu = MainMenu; }));

            InGameMenu.EscapePressed = () => { CurrentMenu = null; };

            OptionsMenu = new Menu(this);
            OptionsMenu.CopyStyling(MainMenu);

            OptionsMenu.AddItem(new Menu.ListItem("Choice:", new string[] { "Option 1", "Option 2", "Option 3" }, (string value) => Console.WriteLine(value + " selected")));
            OptionsMenu.AddItem(new Menu.TextEntryItem("Name:", config.FindValueOrDefault(GameClient.config_ClientName, GameClient.defaultClientName), 12, PlayerNameChanged));
            OptionsMenu.AddItem(new Menu.LinkItem("Back", () => { config.SaveToFile(GameClient.settingsFilename); CurrentMenu = MainMenu; }));

            OptionsMenu.EscapePressed = () => { CurrentMenu = MainMenu; };
        }

        private void OnKeyPressed(object sender, KeyEventArgs e) { CurrentInput.KeyPressed(e); }
        private void OnKeyReleased(object sender, KeyEventArgs e) { CurrentInput.KeyReleased(e); }
        private void OnTextEntered(object sender, TextEventArgs e) { CurrentInput.TextEntered(e); }
        private void OnMousePressed(object sender, MouseButtonEventArgs e) { CurrentInput.MousePressed(e); }
        private void OnMouseReleased(object sender, MouseButtonEventArgs e) { CurrentInput.MouseReleased(e); }
        private void OnMouseMoved(object sender, MouseMoveEventArgs e) { CurrentInput.MouseMoved(e); }
        private void OnClosed(object sender, EventArgs e) { CloseGameWindow(); }

        private void LoadConfig()
        {
            config = Config.ReadFile(GameClient.settingsFilename) ?? GameClient.CreateDefaultConfig();

            // validate the player name, set to default if needed.
            Config value = config.Find(GameClient.config_ClientName);
            string strName = value == null ? string.Empty : value.Value.Trim();
            if (strName == string.Empty)
                strName = GameClient.defaultClientName;
            value.Value = strName;
        }

        private void PlayerNameChanged(string name)
        {
            if (name.Trim() == string.Empty)
                name = GameClient.defaultClientName;

            config.Find("name").Value = name;
            gameClient.Name.Value = name;
        }

        public void CloseGameWindow()
        {
            Close();
            EndGame();
        }

        private void EndGame()
        {
            if ( gameClient.Connected )
                gameClient.Disconnect();
        }
    }
}
